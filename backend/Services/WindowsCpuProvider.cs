using GSInteractiveDeviceAnalyzer.Interfaces;
using System.Diagnostics;
using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Services
{
    public class WindowsCpuProvider : ICpuMetricsProvider
    {
#if WINDOWS
        private readonly List<PerformanceCounter> _coreCounters = new();
        private double _previousAverage = 0;

        public WindowsCpuProvider()
        {
            for(int i = 0; i < Environment.ProcessorCount; i++)
            {
                var pc = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                pc.NextValue();
                _coreCounters.Add(pc);
            }
        }
#else
        public WindowsCpuProvider() =>
            throw new PlatformNotSupportedException("WindowsCpuProvider is only supported on Windows.");
#endif

        public async Task<CpuTelemetryDto> GetNextSampleAsync()
        {
#if WINDOWS
            var loads = _coreCounters.Select(c => Math.Round(c.NextValue(), 1)).ToList();
            double currentAverage = Math.Round(loads.Average(), 1);

            var dto = new CpuTelemetryDto
            {
                AverageLoad = currentAverage,
                Delta = Math.Round(currentAverage - _previousAverage, 1)
            }
            ;
            for (int i = 0; i < loads.Count; i += 4)
            {
                int end = Math.Min(i + 3, loads.Count - 1);
                dto.CoreGroups.Add($"CORE {i}-{end}", loads.Skip(i).Take(4).ToList());
            }

            _previousAverage = currentAverage;
            return await Task.FromResult(dto);
#else

            return await Task.FromException<CpuTelemetryDto>(new PlatformNotSupportedException());
#endif
        }
    }
}
