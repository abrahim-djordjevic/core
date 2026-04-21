using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Services
{
    public interface IDiskOperationService
    {
        DriveTelemetryDto GetDriveTelemetry(string driveLetter);
    }


    public class DiskOperationsService : IDiskOperationService
    {
        public DriveTelemetryDto GetDriveTelemetry(string driveLetter)
        {
            var drive = new DriveInfo(driveLetter);

            long total = drive.TotalSize;
            long free = drive.AvailableFreeSpace;
            long used = total - free;

            return new DriveTelemetryDto
            {
                TotalBytes = total,
                FreeBytes = free,
                UsedBytes = used,
                PercentageFree = Math.Round((double)free / total * 100, 1)
            };
        }
    }
}
