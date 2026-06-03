using GSInteractiveDeviceAnalyzer.Services;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface IDellOemFanReader
    {
        DellFanReading TryGetDellOemFans();
    }
}
