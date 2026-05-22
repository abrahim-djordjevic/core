using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface IThermalProvider : IDisposable
    {
        Task<ThermalTelemetryDto> GetThermalDataAsync();
    }
}
