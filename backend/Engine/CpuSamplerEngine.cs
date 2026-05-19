using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace GSInteractiveDeviceAnalyzer.Engine
{
    public class CpuSamplerEngine : BackgroundService
    {
        private readonly ICpuMetricsProvider _cpuProvider;
        private readonly IHubContext<SystemHub> _hubContext;

        public CpuSamplerEngine(ICpuMetricsProvider cpuProvider, IHubContext<SystemHub> hubContext)
        {
            _cpuProvider = cpuProvider;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var telemetry = await _cpuProvider.GetNextSampleAsync();

                    await _hubContext.Clients.All.SendAsync("ReceiveCpuTelemetry", telemetry, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CPU SAMPLER FAULT] {ex.Message}");
                }
            }
        }
    }
}
