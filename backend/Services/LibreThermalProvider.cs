using BlackSharp.Core.Extensions;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Models;
using System.Runtime.Versioning;
#if WINDOWS
using LibreHardwareMonitor.Hardware;
#endif

namespace GSInteractiveDeviceAnalyzer.Services
{
#if WINDOWS
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();

            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor) {}

        public void VisitParameter(IParameter parameter) { }
    }
    public class LibreThermalProvider : IThermalProvider
    {
        private readonly Computer _computer;
        private readonly UpdateVisitor _visitor;

        public LibreThermalProvider()
        {
            _visitor = new UpdateVisitor();
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMotherboardEnabled = true,
                IsStorageEnabled = true,
                IsControllerEnabled = true
            };

            _computer.Open();
        }

        public async Task<ThermalTelemetryDto> GetThermalDataAsync()
        {
            return await Task.Run(() =>
            {
                var payload = new ThermalTelemetryDto();

                _computer.Accept(_visitor);

                foreach (var hardware in _computer.Hardware)
                {
                    hardware.Update();
                    // CPU Sensors
                    if (hardware.HardwareType == HardwareType.Cpu)
                    {
                        foreach (var sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature)
                            {
                                if (!sensor.Value.HasValue) continue;

                                var temp = Math.Round(sensor.Value.Value, 1);

                                if (sensor.Name.Contains("Package") || sensor.Name.Contains("Tctl/Tdie"))
                                {
                                    payload.CpuPackageCelsius = temp;
                                }
                                else if (sensor.Name.StartsWith("CPU Core #") || (sensor.Name.StartsWith("Core #") &&
                                             !sensor.Name.Contains("Distance")))
                                {
                                    payload.CoreCelsius!.Add(temp);
                                }
                            }
                            else if (sensor.SensorType == SensorType.Power && sensor.Name.Contains("Package") && sensor.Value.HasValue)
                            {
                                payload.CpuPowerWatts = Math.Round(sensor.Value.Value, 1);
                            }
                            else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("Core"))
                            {
                                // Basic throttling check: if clock drops wildly under load(refine it later)
                            }
                        }
                    }

                    // GPU Sensors
                    if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
                    {
                        foreach (var sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature)
                            {
                                if (sensor.Name.Contains("Core") || sensor.Name == "GPU Core")
                                    payload.GpuCoreCelsius = Math.Round(sensor.Value ?? 0, 1);
                                else if (sensor.Name.Contains("Hot Spot"))
                                    payload.GpuHotSpotCelsius = Math.Round(sensor.Value ?? 0, 1);
                                else if (sensor.Name.Contains("Memory") || sensor.Name == "GPU Memory")
                                    payload.GpuVramCelsius = Math.Round(sensor.Value ?? 0, 1);
                            }
                            else if (sensor.SensorType == SensorType.Fan)
                            {
                                payload.GpuFanRpm = (int)(sensor.Value ?? 0);
                            }
                        }
                    }

                    // Motherboard Sensors
                    if (hardware.HardwareType == HardwareType.Motherboard)
                    {
                        foreach (var subHw in hardware.SubHardware)
                        {
                            foreach (var sensor in subHw.Sensors)
                            {
                                if (!sensor.Value.HasValue) continue;

                                if (sensor.SensorType == SensorType.Temperature)
                                {
                                    var temp = Math.Round(sensor.Value.Value, 1);
                                    if (sensor.Name.Contains("System") || sensor.Name.Contains("Motherboard"))
                                        payload.MotherboardCelsius = temp;
                                    if (sensor.Name.Contains("Chipset")) payload.ChipsetCelsius = temp;
                                }
                                else if (sensor.SensorType == SensorType.Fan)
                                {
                                    int rpm = (int)sensor.Value.Value;
                                    if (rpm <= 0) continue;

                                    if (sensor.Name.Contains("CPU")) payload.CpuFanRpm = rpm;
                                    else if (sensor.Name.Contains("Pump") || sensor.Name.Contains("AIO"))
                                        payload.PumpRpm = rpm;
                                    else if (sensor.Name.Contains("Chassis") || sensor.Name.Contains("System"))
                                    {
                                        if (payload.ChassisFan1Rpm == null) payload.ChassisFan1Rpm = rpm;
                                        else payload.ChassisFan2Rpm = rpm;
                                    }
                                }
                            }
                        }
                    }

                    // NVMe / STORAGE
                    if (hardware.HardwareType == HardwareType.Storage)
                    {
                        foreach (var sensor in hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature))
                        {
                            var temp = Math.Round(sensor.Value ?? 0, 1);
                            if (payload.NvmeCelsius == null || temp > payload.NvmeCelsius)
                            {
                                payload.NvmeCelsius = temp;
                            }
                        }
                    }
                }

                if (payload.CpuPackageCelsius == 0 || payload.CpuPackageCelsius == null)
                {
                    try
                    {
                        using var searcher = new System.Management.ManagementObjectSearcher("root\\wmi", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
                        foreach (System.Management.ManagementObject obj in searcher.Get())
                        {
                            var tempK = Convert.ToDouble(obj["CurrentTemperature"]);
                            var celsius = (tempK / 10.0) - 273.15;

                            // Sanity check: ensure the ACPI zone is returning a real temperature
                            if (celsius > 10 && celsius < 120)
                            {
                                payload.CpuPackageCelsius = Math.Round(celsius, 1);
                                break; // Grab the first valid motherboard thermal zone and run
                            }
                        }
                    }
                    catch { /* WMI is completely locked */ }
                }

                return payload;
            });
        }

        public void Dispose()
        {
            _computer.Close();
        }
    }
#else
    public class LibreThermalProvider : IThermalProvider
    {
        public Task<ThermalTelemetryDto> GetThermalDataAsync() =>
            throw new PlatformNotSupportedException("LibreThermalProvider is only supported on Windows.");

        public void Dispose() { }
    }
#endif
}
