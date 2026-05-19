using System.Diagnostics;
using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Models;
using Microsoft.AspNetCore.SignalR;

namespace GSInteractiveDeviceAnalyzer.Engine
{
    public class RamMonitoringEngine
    {
        private CancellationTokenSource? _radarCts;
        private readonly IHubContext<SystemHub> _hub;
        private readonly object _lock = new object();

        public RamMonitoringEngine(IHubContext<SystemHub> hub)
        {
            _hub = hub;
        }

        public void StartRadar()
        {
            lock (_lock)
            {
                if (_radarCts != null && !_radarCts.IsCancellationRequested) return;

                _radarCts = new CancellationTokenSource();
                _ = RadarLoopAsync(_radarCts.Token);
                Console.WriteLine("\n[RAM RADAR] ONLINE: 2-Second Sweep Initiated.");
            }
        }

        public void StopRadar()
        {
            _radarCts?.Cancel();
            Console.WriteLine("\n[RAM RADAR] OFFLINE.");
        }

        private async Task RadarLoopAsync(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    var snapshot = GetTopProcesses(50);
                    var globalMetrics = SystemMemoryMetrics.GetLiveMetrics();

                    var payload = new
                    {
                        Global = globalMetrics ?? new
                        {
                            activeGb = 0.0,
                            cachedGb = 0.0,
                            swapGb = 0.0,
                            totalGb = 16.0,
                        },
                        Processes = snapshot
                    };

                    await _hub.Clients.All.SendAsync("RamTelemetryUpdate", payload, cancellationToken: token);

                    Console.WriteLine($"[RAM SWEEP {DateTime.Now:HH:mm:ss}] Engine Fired - Sent {snapshot.Count} processes");
                }
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[RAM ENGINE FATAL RADAR] ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private List<ProcessTelemetry> GetTopProcesses(int limit)
        {
            var activeProcesses = Process.GetProcesses();
            var telemetryList = new List<ProcessTelemetry>(activeProcesses.Length);

            foreach (var p in activeProcesses)
            {
                try
                {
                    telemetryList.Add(new ProcessTelemetry
                    {
                        ProcessId = p.Id,
                        Name = p.ProcessName,
                        WorkingSetBytes = p.WorkingSet64
                    });
                }
                catch
                {

                }
                finally
                {
                    p.Dispose();
                }
            }

            return telemetryList
                .OrderByDescending(p => p.WorkingSetBytes)
                .Take(limit)
                .ToList();
        }

        public int ExecuteOrder66(List<int> processIds)
        {
            int killCount = 0;
            foreach (var pid in processIds)
            {
                try
                {
                    var process = Process.GetProcessById(pid);
                    process.Kill();
                    killCount++;
                    Console.WriteLine($"[RAM RADAR] ASSASSINATED PID: {pid}");
                }
                catch
                {
                 
                }
            }
            Console.WriteLine($"\n[RAM RADAR] TOTAL ASSASSINATIONS: {killCount} targets eliminated.");
            return killCount;
        }
    }
}
