namespace GSInteractiveDeviceAnalyzer.Services;

public interface IWmiThermalFallback
{
    double? GetCpuTemperatureCelsius();
}