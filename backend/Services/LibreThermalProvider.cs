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
        private readonly IComputerEngine _computer;
        private readonly UpdateVisitor _visitor;
        private readonly IWmiThermalFallback _wmiFallback;

        private ThermalTelemetryDto _lastGoodPayLoad = new ThermalTelemetryDto();
        private float _maxObservedClock = 0f;

        public LibreThermalProvider(IComputerEngine? computer = null, IWmiThermalFallback? wmiFallback = null)
        {
            _visitor = new UpdateVisitor();
            _wmiFallback = wmiFallback ?? new WmiThermalFallback();

            if (computer != null)
            {
                // TEST ENVIRONMENT
                _computer = computer;
            }
            else
            {
                // PRODUCTION ENVIRONMENT
                _computer = new LibreComputerWrapper();
                _computer.Open();
            }
        }

        public async Task<ThermalTelemetryDto> GetThermalDataAsync()
        {
            return await Task.Run(() =>
            {
                var payload = new ThermalTelemetryDto
                {
                    CpuFanRpm = 0,
                    ChassisFan1Rpm = 0,
                    ChassisFan2Rpm = 0,
                    CoreCelsius = new List<double>(),
                    IsThermalThrottling = false
                };
                try
                {
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
                                else if (sensor.SensorType == SensorType.Clock &&
                                         (sensor.Name == "CPU Core #0" || sensor.Name == "CPU Core #1"))
                                {
                                    var currentClock = sensor.Value.Value;

                                    // Track the highest turbo frequency ever seen
                                    if (currentClock > _maxObservedClock) _maxObservedClock = currentClock;

                                    // If clock drops below 80% of the max rated speed, flag it!
                                    if (_maxObservedClock > 0 && currentClock < (_maxObservedClock * 0.8f))
                                    {
                                        payload.IsThermalThrottling = true;
                                    }
                                }
                            }
                        }

                        // GPU Sensors
                        if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
                        {
                            foreach (var sensor in hardware.Sensors)
                            {
                                if (!sensor.Value.HasValue) continue; //  ADDED NULL PROTECTION

                                if (sensor.SensorType == SensorType.Temperature)
                                {
                                    if (sensor.Name.Contains("Core") || sensor.Name == "GPU Core")
                                        payload.GpuCoreCelsius = Math.Round(sensor.Value.Value, 1);
                                    else if (sensor.Name.Contains("Hot Spot"))
                                        payload.GpuHotSpotCelsius = Math.Round(sensor.Value.Value, 1);
                                    else if (sensor.Name.Contains("Memory") || sensor.Name == "GPU Memory")
                                        payload.GpuVramCelsius = Math.Round(sensor.Value.Value, 1);
                                }
                                else if (sensor.SensorType == SensorType.Fan)
                                {
                                    payload.GpuFanRpm = (int)sensor.Value.Value;
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
                                if (!sensor.Value.HasValue) continue; //  ADDED NULL PROTECTION

                                var temp = Math.Round(sensor.Value.Value, 1);
                                if (payload.NvmeCelsius == null || temp > payload.NvmeCelsius)
                                {
                                    payload.NvmeCelsius = temp;
                                }
                            }
                        }
                    }

                    // DELL VBS / WMI FALLBACK
                    if (payload.CpuPackageCelsius == 0 || payload.CpuPackageCelsius == null)
                    {
                        payload.CpuPackageCelsius = _wmiFallback.GetCpuTemperatureCelsius();
                    }

                    _lastGoodPayLoad = payload;
                    return payload;
                }
                catch (Exception )
                {
                    return _lastGoodPayLoad;
                }
                
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
