using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Models;

#if !WINDOWS
using System.IO;
#endif

namespace GSInteractiveDeviceAnalyzer.Services
{
    public class LinuxCpuProvider : ICpuMetricsProvider
    {
#if !WINDOWS
        // Stores the previous tick data for every core to calculate the delta
        private readonly Dictionary<string, (long Idle, long Total)> _prevCoreData = new();
        private double _previousAverage = 0;

        public LinuxCpuProvider()
        {
            // Do an initial read to populate the baseline data, just like discarding the first PerformanceCounter read
            ReadProcStat();
        }

        public Task<CpuTelemetryDto> GetNextSampleAsync()
        {
            var dto = new CpuTelemetryDto();
            var currentData = ReadProcStat();
            var currentCoreLoads = new List<double>();

            foreach (var core in currentData)
            {
                string coreId = core.Key;
                long currentIdle = core.Value.Idle;
                long currentTotal = core.Value.Total;

                if (_prevCoreData.TryGetValue(coreId, out var prev))
                {
                    long totalDelta = currentTotal - prev.Total;
                    long idleDelta = currentIdle - prev.Idle;

                    double load = 0;
                    if (totalDelta > 0)
                    {
                        // The Linux CPU % Formula
                        load = (1.0 - ((double)idleDelta / totalDelta)) * 100.0;
                    }
                    currentCoreLoads.Add(Math.Clamp(Math.Round(load, 1), 0, 100));
                }
                else
                {
                    currentCoreLoads.Add(0); // Fallback for the very first tick
                }

                // Save current state for the next tick's math
                _prevCoreData[coreId] = (currentIdle, currentTotal);
            }

            if (currentCoreLoads.Count > 0)
            {
                // 1. Averages and Deltas
                double currentAverage = Math.Round(currentCoreLoads.Average(), 1);
                dto.AverageLoad = currentAverage;
                dto.Delta = Math.Round(currentAverage - _previousAverage, 1);
                _previousAverage = currentAverage;

                // 2. Dynamic Grouping Algorithm (Chunks of 4)
                for (int i = 0; i < currentCoreLoads.Count; i += 4)
                {
                    int end = Math.Min(i + 3, currentCoreLoads.Count - 1);
                    dto.CoreGroups.Add($"CORE {i}-{end}", currentCoreLoads.Skip(i).Take(4).ToList());
                }
            }

            return Task.FromResult(dto);
        }

        private Dictionary<string, (long Idle, long Total)> ReadProcStat()
        {
            var result = new Dictionary<string, (long Idle, long Total)>();
            try
            {
                var lines = File.ReadAllLines("/proc/stat");
                foreach (var line in lines)
                {
                    // We only want the individual cores: cpu0, cpu1, cpu2, etc.
                    if (line.StartsWith("cpu") && line.Length > 3 && char.IsDigit(line[3]))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        string coreId = parts[0];

                        // /proc/stat columns: user nice system idle iowait irq softirq steal guest
                        long user = long.Parse(parts[1]);
                        long nice = long.Parse(parts[2]);
                        long system = long.Parse(parts[3]);
                        long idle = long.Parse(parts[4]);
                        long iowait = parts.Length > 5 ? long.Parse(parts[5]) : 0;
                        long irq = parts.Length > 6 ? long.Parse(parts[6]) : 0;
                        long softirq = parts.Length > 7 ? long.Parse(parts[7]) : 0;
                        long steal = parts.Length > 8 ? long.Parse(parts[8]) : 0;

                        long totalIdle = idle + iowait;
                        long nonIdle = user + nice + system + irq + softirq + steal;
                        long total = totalIdle + nonIdle;

                        result[coreId] = (totalIdle, total);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LINUX CPU FAULT] Could not read /proc/stat: {ex.Message}");
            }
            return result;
        }
#else
        public Task<CpuTelemetryDto> GetNextSampleAsync() =>
            throw new PlatformNotSupportedException("LinuxCpuProvider is only supported on Linux.");
#endif
    }
}