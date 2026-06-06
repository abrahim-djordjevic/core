using GSInteractiveDeviceAnalyzer.Models;
using DellOemTelemetry = GSInteractiveDeviceAnalyzer.Services.Oem.Dell.DellOemTelemetry;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface IDellOemTelemetry
    {
        DellOemDto? TryGetDellOemTelemetry();
    }
}
