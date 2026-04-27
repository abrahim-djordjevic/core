using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Interfaces;

public interface IDiskOperationService
{
    DriveTelemetryDto GetDriveTelemetry(string driveLetter);
    NukeResultDto ObliterateNode(List<string> path);
    IEnumerable<StorageNode> ScanDirectory(string path);
}