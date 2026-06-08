using System.Runtime.InteropServices;
using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Models;
using GSInteractiveDeviceAnalyzer.Services;
using Microsoft.AspNetCore.SignalR;

namespace GSInteractiveDeviceAnalyzer.Engine
{
    public class ThermalMonitoringEngine : BackgroundService
    {
        private readonly IHubContext<SystemHub> _hubContext;
        private readonly IThermalProvider _thermalProvider;
        private TimeSpan _pollInterval;

        public ThermalMonitoringEngine(IHubContext<SystemHub> hubContext, IThermalProvider thermalProvider, ISettingService settings)
        {
            _hubContext = hubContext;
            _thermalProvider = thermalProvider;
            
            _pollInterval = TimeSpan.FromMilliseconds(settings.Current.Monitoring.ThermalPollIntervalMs);

            settings.OnSettingsChanged += (_, s) => _pollInterval = TimeSpan.FromMilliseconds(s.Monitoring.ThermalPollIntervalMs);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(5000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ThermalTelemetryDto payload = null;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        payload = await _thermalProvider.GetThermalDataAsync();
                    }
                    else
                    {
                        payload = new ThermalTelemetryDto();
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

                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }
}
