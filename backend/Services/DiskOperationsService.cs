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
                        File.Delete(path);
                    }
                    else if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
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

        private void InvalidateCache(string path)
        {
            var normalizedPath = Path.GetFullPath(path);

            string pathWithSlash = normalizedPath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? normalizedPath : normalizedPath + Path.DirectorySeparatorChar;

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

            _scanner.CalculateMissingSizesAsync(items).GetAwaiter().GetResult();

            return items.Select(item =>
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
                else if (item is DirectoryInfo d && _scanner.DirectorySizeCache.TryGetValue(d.FullName, out var cachedSize))
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
            });
        }

        public void TriggerAbort()
        {
            _scanner.TriggerAbort();
        }
    }
}
