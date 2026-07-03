using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces;

public interface IDiskOperationService
{
    DriveTelemetryDto GetDriveTelemetry(string driveLetter);
    IEnumerable<StorageNode> ScanDirectory(string path);

    /// <summary>
    /// Begins a new scan session: cancels any in-flight scan and returns the
    /// cancellation token that ScanDirectory / duplicate detection will observe.
    /// </summary>
    CancellationToken BeginScan();

    void TriggerScanAbort();
}