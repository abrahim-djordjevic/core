using System;

namespace GSInteractiveDeviceAnalyzer.Services
{
    public class WmiThermalFallback : IWmiThermalFallback
    {
        public double? GetCpuTemperatureCelsius()
        {
            try
            {
#pragma warning disable CA1416 
                using var searcher = new System.Management.ManagementObjectSearcher("root\\wmi", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    var tempK = Convert.ToDouble(obj["CurrentTemperature"]);
                    var validTemp = ConvertKelvinToCelsius(tempK);

                    if (validTemp.HasValue)
                    {
                        return validTemp;
                    }
                }
#pragma warning restore CA1416
            }
            catch (Exception ex)
            {
                // 🚀 UNMASKING THE GHOST: Print the exact assassination report to the terminal!
                Console.WriteLine($"\n[DEFENSE GRID WMI CRASH] -> {ex.GetType().Name}: {ex.Message}\n");
            }

            return null;
        }

        public static double? ConvertKelvinToCelsius(double tempK)
        {
            var celsius = (tempK / 10.0) - 273.15;

            if (celsius > 10 && celsius < 120)
            {
                return Math.Round(celsius, 1);
            }
            return null;
        }
    }
}