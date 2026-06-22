using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace GSInteractiveDeviceAnalyzer.Engine
{
    public class CpuSamplerEngine : BackgroundService
    {
        private readonly ICpuMetricsProvider _cpuProvider;
        private readonly IHubContext<SystemHub> _hubContext;
        private TimeSpan _pollInterval;

        public CpuSamplerEngine(ICpuMetricsProvider cpuProvider, IHubContext<SystemHub> hubContext, ISettingService settings)
        {
            _cpuProvider = cpuProvider;
            _hubContext = hubContext;

            _pollInterval = TimeSpan.FromMilliseconds(settings.Current.Monitoring.CpuPollIntervalMs);
            settings.OnSettingsChanged += (_, s) =>
            {
                _pollInterval = TimeSpan.FromMilliseconds(s.Monitoring.CpuPollIntervalMs);
                Console.WriteLine($"[CPU SAMPLER] Poll interval updated → {_pollInterval.TotalMilliseconds}ms");
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var telemetry = await _cpuProvider.GetNextSampleAsync();
                    await _hubContext.Clients.All.SendAsync("ReceiveCpuTelemetry", telemetry, cancellationToken: stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CPU SAMPLER FAULT] {ex.Message}");
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }
}