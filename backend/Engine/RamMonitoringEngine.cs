using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Models;
using Microsoft.AspNetCore.SignalR;

namespace GSInteractiveDeviceAnalyzer.Engine
{
    public class RamMonitoringEngine
    {
        private CancellationTokenSource? _radarCts;
        private readonly IHubContext<SystemHub> _hub;
        private readonly IProcessOwnerResolver _ownerResolver;
        private readonly object _lock = new object();
        private TimeSpan _pollInterval;

        // Per-process CPU baseline from the previous tick
        private readonly ConcurrentDictionary<int, (TimeSpan CpuTime, DateTime Timestamp)> _prevCpuSnapshot = new();

        // Consecutive zero-CPU-tick counter per PID for status heuristic
        private readonly ConcurrentDictionary<int, int> _zeroTickCounts = new();

        public RamMonitoringEngine(IHubContext<SystemHub> hub, ISettingService settings, IProcessOwnerResolver ownerResolver)
        {
            _hub = hub;
            _ownerResolver = ownerResolver;

            _pollInterval = TimeSpan.FromMilliseconds(settings.Current.Monitoring.RamPollIntervalMs);

            settings.OnSettingsChanged += (_, s) => _pollInterval =
                TimeSpan.FromMilliseconds(s.Monitoring.RamPollIntervalMs);
        }

        public void StartRadar()
        {
            lock (_lock)
            {
                if (_radarCts != null && !_radarCts.IsCancellationRequested) return;

                _radarCts = new CancellationTokenSource();
                _ = RadarLoopAsync(_radarCts.Token);
                Console.WriteLine("\n[RAM RADAR] ONLINE: Process Explorer Sweep Initiated.");
            }
        }

        public void StopRadar()
        {
            _radarCts?.Cancel();
            Console.WriteLine("\n[RAM RADAR] OFFLINE.");
        }

        private async Task RadarLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Refresh the owner cache once per tick — one WMI/proc round-trip
                    _ownerResolver.RefreshCache();

                    var snapshot = GetTopProcesses(100);
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

                    await _hub.Clients.All.SendAsync("RamUpdate", payload, cancellationToken: token);

                    Console.WriteLine($"[RAM SWEEP {DateTime.Now:HH:mm:ss}] Engine Fired - Sent {snapshot.Count} processes");

                    await Task.Delay(_pollInterval, token);
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
            var seenPids = new HashSet<int>(activeProcesses.Length);
            var now = DateTime.UtcNow;

            foreach (var p in activeProcesses)
            {
                try
                {
                    seenPids.Add(p.Id);

                    // CPU% — delta between two consecutive ticks
                    double cpuPercent = 0.0;
                    try
                    {
                        var currentCpuTime = p.TotalProcessorTime;

                        if (_prevCpuSnapshot.TryGetValue(p.Id, out var prev))
                        {
                            var elapsedMs = (now - prev.Timestamp).TotalMilliseconds;
                            if (elapsedMs > 0)
                            {
                                var cpuMs = (currentCpuTime - prev.CpuTime).TotalMilliseconds;
                                cpuPercent = cpuMs / (elapsedMs * Environment.ProcessorCount) * 100.0;
                                cpuPercent = Math.Clamp(cpuPercent, 0.0, 100.0);
                                cpuPercent = Math.Round(cpuPercent, 1);
                            }
                        }
                        // else: first tick for this PID — emit 0

                        _prevCpuSnapshot[p.Id] = (currentCpuTime, now);
                    }
                    catch
                    {
                        // TotalProcessorTime throws on some system processes — emit 0
                        _prevCpuSnapshot[p.Id] = (TimeSpan.Zero, now);
                    }

                    // STATUS — heuristic using zero-tick counter
                    string status;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        status = GetLinuxProcessStatus(p.Id);
                    }
                    else
                    {
                        status = GetStatusHeuristic(p, cpuPercent);
                    }

                    // USER — resolved from the batch-refreshed cache
                    var user = _ownerResolver.Resolve(p.Id);

                    telemetryList.Add(new ProcessTelemetry
                    {
                        ProcessId = p.Id,
                        Name = p.ProcessName,
                        WorkingSetBytes = p.WorkingSet64,
                        CpuPercent = cpuPercent,
                        Status = status,
                        User = user
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

            // Prune stale entries — PIDs that no longer exist
            foreach (var stalePid in _prevCpuSnapshot.Keys.Where(pid => !seenPids.Contains(pid)).ToList())
            {
                _prevCpuSnapshot.TryRemove(stalePid, out _);
                _zeroTickCounts.TryRemove(stalePid, out _);
            }

            // Sort: primary by CPU% descending, secondary by RAM descending for ties
            var sortedList = telemetryList
                .OrderByDescending(p => p.CpuPercent)
                .ThenByDescending(p => p.WorkingSetBytes)
                .Take(limit)
                .ToList();

            return sortedList;
        }

        private string GetStatusHeuristic(Process p, double cpuPercent)
        {
            try
            {
                if (p.HasExited) return "STOPPED";
            }
            catch
            {
                // HasExited throws on access-denied processes — treat as running
            }

            if (cpuPercent == 0.0)
            {
                var count = _zeroTickCounts.AddOrUpdate(p.Id, 1, (_, prev) => prev + 1);
                if (count >= 5) return "SLEEPING";
            }
            else
            {
                _zeroTickCounts[p.Id] = 0;
            }

            return "RUNNING";
        }

        private string GetLinuxProcessStatus(int pid)
        {
#if !WINDOWS
            try
            {
                var statPath = $"/proc/{pid}/stat";
                if (!File.Exists(statPath)) return "STOPPED";

                var content = File.ReadAllText(statPath);

                // The state character is field 3, but field 2 (comm) can contain spaces and parens
                // Find the last ')' to skip the comm field safely
                var lastParen = content.LastIndexOf(')');
                if (lastParen < 0 || lastParen + 2 >= content.Length) return "RUNNING";

                var stateChar = content[lastParen + 2]; // space + state character

                return stateChar switch
                {
                    'R' => "RUNNING",
                    'S' or 'D' or 'I' => "SLEEPING",
                    'T' or 't' => "STOPPED",
                    'Z' => "ZOMBIE",
                    _ => "RUNNING"
                };
            }
            catch
            {
                return "RUNNING";
            }
#else
            return "RUNNING";
#endif
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
