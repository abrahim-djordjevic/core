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

        foreach (var path in paths)
        {
            if (cancelToken.IsCancellationRequested)
            {
                await _hubContext.Clients.All.SendAsync("NukeAborted", "OPERATION ABORTED BY USER");
                return new NukeResultDto { Message = "PARTIAL NUKE: ABORTED" };
            }
            try
            {
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    AggressiveObliterate(path);
                }
                else
                {
                    continue;
                }

                InvalidateCache(path);

                processedNodes++;
                var percentage = Math.Round(((double)processedNodes / totalNodes) * 100, 1);

                await _hubContext.Clients.All.SendAsync("NukeProgress", new
                {
                    completed = processedNodes,
                    total = totalNodes,
                    percentage = percentage,
                    currentTarget = Path.GetFileName(path)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NUKE ERROR] Failed to Nuke {path}: {ex.Message}");
            }
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

    private void InvalidateCache(string path)
    {
        var normalizedPath = Path.GetFullPath(path);

        var pathWithSlash = normalizedPath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? normalizedPath : normalizedPath + Path.DirectorySeparatorChar;

        var keysToRemove = _scanner.DirectorySizeCache.Keys
            .Where(k => k.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase) || k.StartsWith(pathWithSlash, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _scanner.DirectorySizeCache.TryRemove(key, out _);
        }

        var parent = Path.GetDirectoryName(normalizedPath);
        while (!string.IsNullOrEmpty(parent))
        {
            _scanner.DirectorySizeCache.TryRemove(parent, out _);
            parent = Path.GetDirectoryName(parent);
        }

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