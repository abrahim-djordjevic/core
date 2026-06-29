using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces;

public interface IDiskOperationService
{
    DriveTelemetryDto GetDriveTelemetry(string driveLetter);
    IEnumerable<StorageNode> ScanDirectory(string path);
    void TriggerScanAbort();
}