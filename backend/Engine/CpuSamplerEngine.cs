using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace GSSystemAnalyzer.Engine
{
    public class CpuSamplerEngine : BackgroundService
    {
        private readonly ICpuMetricsProvider _cpuProvider;
        private readonly IHubContext<SystemHub> _hubContext;
        private readonly ITelemetryHistoryBuffer _historyBuffer;
        private TimeSpan _pollInterval;

        public CpuSamplerEngine(ICpuMetricsProvider cpuProvider, IHubContext<SystemHub> hubContext, ISettingService settings, ITelemetryHistoryBuffer historyBuffer)
        {
            _cpuProvider = cpuProvider;
            _hubContext = hubContext;
            _historyBuffer = historyBuffer;

            _pollInterval = TimeSpan.FromMilliseconds(settings.Current.Monitoring.CpuPollIntervalMs);
            settings.OnSettingsChanged += (_, s) =>
            {
                _pollInterval = TimeSpan.FromMilliseconds(s.Monitoring.CpuPollIntervalMs);
                Console.WriteLine($"[CPU SAMPLER] Poll interval updated ? {_pollInterval.TotalMilliseconds}ms");
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

                    // Record to history buffer for historical charts
                    _historyBuffer.Record("cpu", telemetry.AverageLoad);
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