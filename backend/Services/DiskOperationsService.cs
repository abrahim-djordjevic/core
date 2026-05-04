using System.Drawing;
using GSInteractiveDeviceAnalyzer.Engine;
using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Models;
using Microsoft.AspNetCore.SignalR;

namespace GSInteractiveDeviceAnalyzer.Services
{
    public class DiskOperationsService : IDiskOperationService
    {
        private readonly DiskScannerEngine _scanner;
        private readonly IHubContext<StorageHub> _hubContext;

        public DiskOperationsService(DiskScannerEngine scanner, IHubContext<StorageHub> hubContext)
        {
            _scanner = scanner;
            _hubContext = hubContext;
        }

        public DriveTelemetryDto GetDriveTelemetry(string driveLetter)
        {
            var drive = new DriveInfo(driveLetter);

            var total = drive.TotalSize;
            var free = drive.TotalFreeSpace;
            var used = total - free;

            return new DriveTelemetryDto
            {
                TotalBytes = total,
                FreeBytes = free,
                UsedBytes = used,
                PercentageFree = Math.Round((double)free / total * 100, 1)
            };
        }

        public async Task<NukeResultDto> ObliterateNode(List<string> paths)
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

        public IEnumerable<StorageNode> ScanDirectory(string path)
        {
            var items = _scanner.LoadDirectoryItems(path);
            PurgeDeadMemory(path, items);
            _scanner.CalculateMissingSizesAsync(items).GetAwaiter().GetResult();

            var nodes = items.Select(item =>
            {
                DateTime safeDate;
                try
                {
                    safeDate = item.LastWriteTime;
                }
                catch
                {
                    safeDate = DateTime.UtcNow;
                }

                long itemSize = 0;
                if (item is FileInfo f) itemSize = f.Length;
                else if (item is DirectoryInfo d &&
                         _scanner.DirectorySizeCache.TryGetValue(d.FullName, out var cachedSize))
                {
                    itemSize = cachedSize.Size;
                }


                return new StorageNode
                {
                    Name = item.Name,
                    Path = item.FullName,
                    Type = item.Attributes.HasFlag(FileAttributes.Directory) ? "Directory" : "File",
                    SizeBytes = itemSize,
                    LastModified = safeDate
                };
            }).ToList();

            var actualFolderSize = nodes.Sum(n => n.SizeBytes);

            var normalizedPath = Path.GetFullPath(path);
            _scanner.DirectorySizeCache[normalizedPath] = new CacheEntry
                { Size = actualFolderSize, LastUpdated = DateTime.UtcNow };

            _scanner.SaveMemoryToDisk();

            return nodes;
        }

        private void PurgeDeadMemory(string currentPath, List<FileSystemInfo> actualItems)
        {
            var memoryChanged = false;

            var actualPaths =
                new HashSet<string>(actualItems.Select(i => i.FullName), StringComparer.OrdinalIgnoreCase);

            var pathWithSlash = currentPath.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? currentPath
                : currentPath + Path.DirectorySeparatorChar;

            var keysToCheck = _scanner.DirectorySizeCache.Keys
                .Where(k => k.StartsWith(pathWithSlash, StringComparison.OrdinalIgnoreCase))
                .Where(k => k.Length >= pathWithSlash.Length && k.IndexOf(Path.DirectorySeparatorChar, pathWithSlash.Length) == -1)
                .ToList();

            foreach (var key in keysToCheck)
            {
                if (!actualPaths.Contains(key))
                {
                    _scanner.DirectorySizeCache.TryRemove(key, out _);
                    memoryChanged = true;
                    Console.WriteLine($"MEMORY PURGED: Removed Ghost Folder -> {key}");
                }
            }

            if (memoryChanged)
            {
                _scanner.SaveMemoryToDisk();
            }
        }

        public void TriggerNukeAbort()
        {
            _scanner.TriggerNukeAbort();
        }

        public void TriggerScanAbort()
        {
            _scanner.TriggerScanAbort();
        }
    }
}
