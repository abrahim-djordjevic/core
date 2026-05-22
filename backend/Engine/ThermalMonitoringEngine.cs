using System.Runtime.InteropServices;
using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Services;
using Microsoft.AspNetCore.SignalR;

namespace GSInteractiveDeviceAnalyzer.Engine
{
    public class ThermalMonitoringEngine : BackgroundService
    {
        private readonly IHubContext<SystemHub> _hubContext;
        private readonly IThermalProvider _thermalProvider;

        public ThermalMonitoringEngine(IHubContext<SystemHub> hubContext, IThermalProvider thermalProvider)
        {
            _hubContext = hubContext;
            _thermalProvider = thermalProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(5000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Models.ThermalTelemetryDto payload = null;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        payload = await _thermalProvider.GetThermalDataAsync();
                    }
                    else
                    {
                        payload = new Models.ThermalTelemetryDto();
                    }

                    if (payload != null)
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveThermalTelemetry", payload, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[THERMAL ENGINE WARN] Radar Failure: {ex.Message}");
                }

                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
