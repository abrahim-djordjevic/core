using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Services
{
    public class DiskOperationsService : IDiskOperationService
    {
        private readonly DiskScannerEngine _scanner;

        public DiskOperationsService(DiskScannerEngine scanner)
        {
            _scanner = scanner;
        }

        public DriveTelemetryDto GetDriveTelemetry(string driveLetter)
        {
            var drive = new DriveInfo(driveLetter);

            var total = drive.TotalSize;
            var free = drive.AvailableFreeSpace;
            var used = total - free;

            return new DriveTelemetryDto
            {
                TotalBytes = total,
                FreeBytes = free,
                UsedBytes = used,
                PercentageFree = Math.Round((double)free / total * 100, 1)
            };
        }

        public NukeResultDto ObliterateNode(string path)
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return new NukeResultDto
                {
                    Message = "TARGET NUKED",
                    Path = path,
                    Type = "File"
                };
            }
            else if (System.IO.Directory.Exists(path))
            {
                System.IO.Directory.Delete(path, true);
                return new NukeResultDto
                {
                    Message = "TARGET NUKED",
                    Path = path,
                    Type = "Directory"
                };
            }
            else
            {
                throw new FileLoadException("TARGET NOT FOUND");
            }
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
    }
}
