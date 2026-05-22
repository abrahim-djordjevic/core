using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GSInteractiveDeviceAnalyzer.Services
{
    public class LinuxThermalProvider : IThermalProvider
    {
        public async Task<ThermalTelemetryDto> GetThermalDataAsync()
        {
            var payload = new ThermalTelemetryDto();

            // Run in a background thread since we are hitting the file system
            await Task.Run(async () =>
            {
                // THERMAL ZONES (Temperatures)
                try
                {
                    if (Directory.Exists("/sys/class/thermal/"))
                    {
                        foreach (var dir in Directory.GetDirectories("/sys/class/thermal/", "thermal_zone*"))
                        {
                            var type = ReadSysFsSafe(Path.Combine(dir, "type"));
                            var tempStr = ReadSysFsSafe(Path.Combine(dir, "temp"));

                            if (type != null && double.TryParse(tempStr, out double milliCelsius))
                            {
                                double celsius = Math.Round(milliCelsius / 1000.0, 1);

                                if (type.Contains("x86_pkg_temp", StringComparison.OrdinalIgnoreCase))
                                    payload.CpuPackageCelsius = celsius;
                                else if (type.Contains("acpitz", StringComparison.OrdinalIgnoreCase))
                                    payload.MotherboardCelsius = celsius;
                                else if (type.Contains("core", StringComparison.OrdinalIgnoreCase))
                                    payload.CoreCelsius!.Add(celsius);
                            }
                        }
                    }
                }
                catch { /* Graceful degradation */ }

                // FANS (RPM)
                try
                {
                    if (Directory.Exists("/sys/class/hwmon/"))
                    {
                        int fanIndex = 0;
                        foreach (var hwmon in Directory.GetDirectories("/sys/class/hwmon/"))
                        {
                            foreach (var fanFile in Directory.GetFiles(hwmon, "fan*_input"))
                            {
                                var rpmStr = ReadSysFsSafe(fanFile);
                                if (int.TryParse(rpmStr, out int rpm))
                                {
                                    if (fanIndex == 0) payload.CpuFanRpm = rpm;
                                    else if (fanIndex == 1) payload.ChassisFan1Rpm = rpm;
                                    else if (fanIndex == 2) payload.ChassisFan2Rpm = rpm;
                                    else if (fanIndex == 3) payload.PumpRpm = rpm;
                                    fanIndex++;
                                }
                            }
                        }
                    }
                }
                catch { /* Graceful degradation */ }

                // THROTTLING (CPU Frequencies)
                try
                {
                    var curFreqStr = ReadSysFsSafe("/sys/devices/system/cpu/cpu0/cpufreq/scaling_cur_freq");
                    var maxFreqStr = ReadSysFsSafe("/sys/devices/system/cpu/cpu0/cpufreq/cpuinfo_max_freq");

                    if (double.TryParse(curFreqStr, out double cur) && double.TryParse(maxFreqStr, out double max))
                    {
                        if (max > 0 && (cur / max) < 0.8) // More than 20% below max
                        {
                            payload.IsThermalThrottling = true;
                        }
                    }
                }
                catch { /* Graceful degradation */ }

                // NVMe TEMPERATURES
                try
                {
                    if (Directory.Exists("/sys/class/nvme/"))
                    {
                        foreach (var nvmeDir in Directory.GetDirectories("/sys/class/nvme/"))
                        {
                            var hwmonPath = Path.Combine(nvmeDir, "hwmon");
                            if (Directory.Exists(hwmonPath))
                            {
                                foreach (var hwmon in Directory.GetDirectories(hwmonPath))
                                {
                                    var tempStr = ReadSysFsSafe(Path.Combine(hwmon, "temp1_input"));
                                    if (double.TryParse(tempStr, out double milliCelsius))
                                    {
                                        double celsius = Math.Round(milliCelsius / 1000.0, 1);
                                        if (payload.NvmeCelsius == null || celsius > payload.NvmeCelsius)
                                        {
                                            payload.NvmeCelsius = celsius; // Keep the hottest drive
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { /* Graceful degradation */ }

                // INTEL RAPL (Power Watts via Delta)
                try
                {
                    string raplPath = "/sys/class/powercap/intel-rapl/intel-rapl:0/energy_uj";
                    var energy1Str = ReadSysFsSafe(raplPath);

                    if (long.TryParse(energy1Str, out long energy1))
                    {
                        // Wait exactly 1 second as specified
                        await Task.Delay(1000);

                        var energy2Str = ReadSysFsSafe(raplPath);
                        if (long.TryParse(energy2Str, out long energy2))
                        {
                            long delta = energy2 - energy1;
                            if (delta > 0)
                            {
                                payload.CpuPowerWatts = Math.Round(delta / 1000000.0, 1);
                            }
                        }
                    }
                }
                catch { /* Graceful degradation */ }
            });

            return payload;
        }

        // Helper method to safely read sysfs files without crashing on locks/permissions
        private string? ReadSysFsSafe(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return File.ReadAllText(path).Trim();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            // No unmanaged resources to clean up for the Linux provider
        }
    }
}