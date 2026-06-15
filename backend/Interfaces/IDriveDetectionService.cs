using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface IDriveDetectionService
    {
        List<DriveMetric> GetReadyDrives();
    }
}