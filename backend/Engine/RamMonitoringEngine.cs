using System.Diagnostics;
using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Models;
using Microsoft.AspNetCore.SignalR;

namespace GSInteractiveDeviceAnalyzer.Engine
{
    public class RamMonitoringEngine
    {
        private CancellationTokenSource? _radarCts;
        private readonly IHubContext<StorageHub> _hub;
        private readonly object _lock = new object();

        public RamMonitoringEngine(IHubContext<StorageHub> hub)
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

                    await _hub.Clients.All.SendAsync("RamTelemetryUpdate", snapshot, cancellationToken: token);

                    Console.WriteLine($"[RAM SWEEP {DateTime.Now:HH:mm:ss}] Top Offender: {snapshot.FirstOrDefault()?.Name} - {snapshot.FirstOrDefault()?.RamMb} MB");
                }
            }
            catch (OperationCanceledException )
            {
                
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

        public bool ExecuteOrder66(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                process.Kill();
                Console.WriteLine($"[RAM RADAR] ASSASSINATED PID: {processId}");
                return true;
            }
            catch (ArgumentException)
            {
                Console.WriteLine($"[RAM RADAR] Target PID {processId} is already dead.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RAM RADAR] ASSASSINATION FAILED for PID {processId}: {ex.Message}");
                return false;
            }
        }
    }
}
