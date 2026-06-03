using System.Management;
using GSInteractiveDeviceAnalyzer.Interfaces;

namespace GSInteractiveDeviceAnalyzer.Services
{
    public sealed class DellFanReading
    {
        public int? CpuFanRpm { get; set; }
        public int? GpuFanRpm { get; set; }
        public int? ChassisFanRpm { get; set; }
    }

    /// <summary>
    /// Tier-3 fallback: reads fan RPM from Dell Command | Monitor's WMI provider
    /// (root\dcim\sysman). No ring-0 driver, so it survives HVCI / Memory Integrity.
    /// Returns null if DCM isn't installed or the namespace is unavailable.
    /// </summary>
    public sealed class DellOemFanReader : IDellOemFanReader
    {
        private static ManagementScope? _scope;

        private static DellFanReading? _cachedReading = null;
        private static DateTime _lastPollTime = DateTime.MinValue;
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(3);
        public DellFanReading? TryGetDellOemFans()
        {
            if (DateTime.UtcNow - _lastPollTime < _cacheDuration && _cachedReading != null)
            {
                return _cachedReading;
            }
            try
            {
                using var searcher = new ManagementObjectSearcher("root\\dcim\\sysman", "SELECT * FROM DCIM_NumericSensor");
                using var results = searcher.Get();

                var reading = new DellFanReading();
                var found = false;

                foreach (ManagementBaseObject o in results)
                {
                    using (o)
                    {
                        if (o["SensorType"] != null && Convert.ToInt32(o["SensorType"]) == 5)
                        {
                            var name = o["ElementName"]?.ToString() ?? "";
                            var raw = Convert.ToInt64(o["CurrentReading"]);
                            var mod = Convert.ToInt32(o["UnitModifier"]);
                            var rpm = (int)(raw * Math.Pow(10, mod));

                            if (name.IndexOf("CPU", StringComparison.OrdinalIgnoreCase) >= 0)
                                reading.CpuFanRpm = rpm;
                            else if (name.IndexOf("GPU", StringComparison.OrdinalIgnoreCase) >= 0)
                                reading.GpuFanRpm = rpm;
                            else if (name.IndexOf("Chassis", StringComparison.OrdinalIgnoreCase) >= 0)
                                reading.ChassisFanRpm = rpm;

                            found = true;
                        }   
                        
                    }
                    
                }

                if (found)
                {
                    _cachedReading = reading;
                    _lastPollTime = DateTime.UtcNow;
                    return _cachedReading;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[DELL OEM FAN CRASH] -> {ex.Message}\n");
                return null;
            }
        }

        public static int CalculateRpm(long rawReading, int unitModifier)
        {
            return (int)(rawReading * Math.Pow(10, unitModifier));
        }
    }
    
}
