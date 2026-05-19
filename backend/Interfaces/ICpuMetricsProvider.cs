using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface ICpuMetricsProvider
    {
        Task<CpuTelemetryDto> GetNextSampleAsync();
    }
}
