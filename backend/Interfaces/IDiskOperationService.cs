using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Interfaces;

public interface IDiskOperationService
{
    DriveTelemetryDto GetDriveTelemetry(string driveLetter);
    NukeResultDto ObliterateNode(string path);
    IEnumerable<StorageNode> ScanDirectory(string path);
}