using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GSInteractiveDeviceAnalyzer.Engine;
using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Models;
using GSInteractiveDeviceAnalyzer.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace GSInteractiveDeviceAnalyzer.Services;

public class NukeProtocolService : INukeProtocolService
{
    private readonly DiskScannerEngine _scanner;
    private readonly IHubContext<SystemHub> _hubContext;

    public NukeProtocolService(DiskScannerEngine scanner, IHubContext<SystemHub> hubContext)
    {
        _scanner = scanner;
        _hubContext = hubContext;
    }
    public async Task<NukePreviewResponse> PreviewNukeAsync(List<string> paths, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var response = new NukePreviewResponse();

            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false
            };

            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                long pathBytes = 0;
                int pathFileCount = 0;

                if (File.Exists(path))
                {
                    // if it's a single file
                    pathBytes = new FileInfo(path).Length;
                    pathFileCount = 1;
                }
                else if (Directory.Exists(path))
                {
                    // The Zero Allocation Counter
                    // We parse the raw OS struct, update the tally, and discard the object instantly
                    var counter = new System.IO.Enumeration.FileSystemEnumerable<byte>(
                        path,
                        (ref System.IO.Enumeration.FileSystemEntry entry) =>
                        {
                            if (!entry.IsDirectory)
                            {
                                pathBytes += entry.Length;
                                pathFileCount++;
                            }
                            return 0;
                        },
                        options
                    );

                    foreach (var _ in counter)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                else
                {
                    // if it's a ghost path
                    continue;
                }
                

                response.Breakdown.Add(new NukePathBreakdown
                {
                    Path = path,
                    SizeBytes = pathBytes,
                    FileCount = pathFileCount
                });

                response.TotalBytes += pathBytes;
                response.TotalFiles += pathFileCount;
            }

            response.TotalFormatted = FormatSize(response.TotalBytes);

            return response;

        }, cancellationToken);
    }

    public async Task<NukeResultDto> ObliterateNodeAsync(List<string> paths)
    {
        var totalNodes = paths.Count;
        var processedNodes = 0;
        var cancelToken = _scanner.NukeToken();

        // Throttle progress so we don't await a hub round-trip per file.
        var lastProgressSent = DateTime.MinValue;
        var progressInterval = TimeSpan.FromMilliseconds(150);

        // Track what we actually removed so the cache is invalidated in ONE pass at the end.
        var nukedPaths = new List<string>();
        var aborted = false;

        foreach (var path in paths)
        {
            if (cancelToken.IsCancellationRequested)
            {
                aborted = true;
                break;
            }

            try
            {
                if (File.Exists(path))
                {
                    // Only clear attributes when needed — skip a syscall on normal files.
                    var attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReadOnly) != 0)
                    {
                        File.SetAttributes(path, FileAttributes.Normal);
                    }
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    AggressiveObliterate(path);
                }
                else
                {
                    continue; // ghost path
                } 
                
                
                InvalidateCacheBatch(new List<string> { path });

                processedNodes++;

                var percentage = Math.Round(((double)processedNodes / totalNodes) * 100, 1);

                // Emit at most every 150ms, but always emit the final tick.
                if (DateTime.UtcNow - lastProgressSent >= progressInterval || processedNodes == totalNodes)
                {
                    lastProgressSent = DateTime.UtcNow;
                    await _hubContext.Clients.All.SendAsync("NukeProgress", new
                    {
                        completed = processedNodes,
                        total = totalNodes,
                        percentage = percentage,
                        currentTarget = Path.GetFileName(path)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NUKE ERROR] Failed to Nuke {path}: {ex.Message}");
            }
        }

        // Invalidate everything we removed in a single cache pass, then persist to disk ONCE.
        InvalidateCacheBatch(nukedPaths);

        if (aborted)
        {
            await _hubContext.Clients.All.SendAsync("NukeAborted", "OPERATION ABORTED BY USER");
            return new NukeResultDto { Message = "PARTIAL NUKE: ABORTED" };
        }

        return new NukeResultDto { Message = "CARPET BOMBING COMPLETE" };
    }

    private void AggressiveObliterate(string targetPath)
    {
        var dir = new DirectoryInfo(targetPath);

        foreach (var info in dir.GetFileSystemInfos("*", SearchOption.AllDirectories))
        {
            info.Attributes = FileAttributes.Normal;
        }

        dir.Attributes = FileAttributes.Normal;
        dir.Delete(true);
    }

    private void InvalidateCacheBatch(IEnumerable<string> paths)
    {
        var nukedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parentsToRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var subtreePrefixes = new List<string>();

        foreach (var path in paths)
        {
            var normalizedPath = Path.GetFullPath(path);
            nukedSet.Add(normalizedPath);

            subtreePrefixes.Add(
                normalizedPath.EndsWith(Path.DirectorySeparatorChar.ToString())
                    ? normalizedPath
                    : normalizedPath + Path.DirectorySeparatorChar);

            // Every ancestor's cached size is now stale.
            var parent = Path.GetDirectoryName(normalizedPath);
            while (!string.IsNullOrEmpty(parent))
            {
                parentsToRemove.Add(parent);
                parent = Path.GetDirectoryName(parent);
            }
        }

        if (nukedSet.Count == 0)
        {
            return;
        }

        // ONE pass over the cache: drop nuked nodes, anything under a nuked directory,
        // and every affected ancestor directory.
        var keysToRemove = _scanner.DirectorySizeCache.Keys
            .Where(k =>
                nukedSet.Contains(k) ||
                parentsToRemove.Contains(k) ||
                subtreePrefixes.Any(prefix => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _scanner.DirectorySizeCache.TryRemove(key, out _);
        }

        // Persist the cache to disk exactly once for the whole batch.
        _scanner.SaveMemoryToDisk();
    }

    private string FormatSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return string.Format("{0:n1} {1}", number, suffixes[counter]);
    }

    public void TriggerNukeAbort()
    {
        _scanner.TriggerNukeAbort();
    }
}