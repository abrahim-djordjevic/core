using System;

namespace GSInteractiveDeviceAnalyzer.Services
{
    // 1. The Interface we can Mock!
    public interface IWmiThermalFallback
    {
        double? GetCpuTemperatureCelsius();
    }

    public class WmiThermalFallback : IWmiThermalFallback
    {
        public double? GetCpuTemperatureCelsius()
        {
            try
            {
#pragma warning disable CA1416 // Suppress OS warning since this only runs on Windows
                using var searcher = new System.Management.ManagementObjectSearcher("root\\wmi", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    var tempK = Convert.ToDouble(obj["CurrentTemperature"]);
                    return ConvertKelvinToCelsius(tempK);
                }
#pragma warning restore CA1416
            }
            catch { /* WMI is completely locked */ }

            return null; // Returns null if no instances/locked
        }

        // 🚀 2. Extracting the math so xUnit can test it securely!
        public static double? ConvertKelvinToCelsius(double tempK)
        {
            var celsius = (tempK / 10.0) - 273.15;

            // Sanity check: ensure the ACPI zone is returning a real temperature
            if (celsius > 10 && celsius < 120)
            {
                return Math.Round(celsius, 1);
            }
            return null;
        }
    }
}