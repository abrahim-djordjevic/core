using System.Collections.Concurrent;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Services
{
    public class TelemetryHistoryBuffer : ITelemetryHistoryBuffer
    {
        // Registry of supported metrics and their display units.
        // Wired metrics are populated by background services.
        // TODO metrics are registered but not yet wired — will return empty points until a provider is built.
        private static readonly Dictionary<string, string> MetricUnits = new()
        {
            ["cpu"]                 = "%",
            ["ram"]                 = "GB",
            ["ram_percent"]         = "%",
            ["thermal_cpu_package"] = "°C",
            ["network_rx"]          = "MB/s",   // TODO: wire when Network provider is built
            ["network_tx"]          = "MB/s",   // TODO: wire when Network provider is built
            ["disk_io_read"]        = "MB/s",   // TODO: wire when Disk I/O provider is built
            ["disk_io_write"]       = "MB/s",   // TODO: wire when Disk I/O provider is built
        };

        private readonly ConcurrentDictionary<string, ConcurrentQueue<TelemetryPoint>> _buffers = new();
        private static readonly TimeSpan MaxRetention = TimeSpan.FromMinutes(60);

        /// <inheritdoc />
        public void Record(string metric, double value)
        {
            if (!MetricUnits.ContainsKey(metric)) return;

            var queue = _buffers.GetOrAdd(metric, _ => new ConcurrentQueue<TelemetryPoint>());

            queue.Enqueue(new TelemetryPoint
            {
                Timestamp = DateTime.UtcNow,
                Value = Math.Round(value, 2)
            });

            Prune(queue);
        }

        /// <inheritdoc />
        public TelemetryHistoryResponse? GetHistory(string metric, int minutes)
        {
            if (!MetricUnits.TryGetValue(metric, out var unit))
                return null;

            minutes = Math.Clamp(minutes, 1, 60);

            var cutoff = DateTime.UtcNow.AddMinutes(-minutes);

            List<TelemetryPoint> points;

            if (_buffers.TryGetValue(metric, out var queue))
            {
                // Snapshot the queue and filter to the requested window
                points = queue.ToArray()
                    .Where(p => p.Timestamp >= cutoff)
                    .OrderBy(p => p.Timestamp)
                    .ToList();
            }
            else
            {
                points = new List<TelemetryPoint>();
            }

            var stats = new TelemetryStats();
            if (points.Count > 0)
            {
                stats.Min     = Math.Round(points.Min(p => p.Value), 2);
                stats.Max     = Math.Round(points.Max(p => p.Value), 2);
                stats.Avg     = Math.Round(points.Average(p => p.Value), 2);
                stats.Current = points[^1].Value;
            }

            return new TelemetryHistoryResponse
            {
                Metric  = metric,
                Minutes = minutes,
                Unit    = unit,
                Points  = points,
                Stats   = stats
            };
        }

        /// <inheritdoc />
        public IReadOnlyCollection<string> GetSupportedMetrics()
        {
            return MetricUnits.Keys.ToList().AsReadOnly();
        }

        /// <summary>
        /// Removes entries older than 60 minutes from the front of the queue.
        /// Safe to call concurrently — ConcurrentQueue.TryDequeue is atomic.
        /// </summary>
        private static void Prune(ConcurrentQueue<TelemetryPoint> queue)
        {
            var horizon = DateTime.UtcNow - MaxRetention;

            while (queue.TryPeek(out var oldest) && oldest.Timestamp < horizon)
            {
                queue.TryDequeue(out _);
            }
        }
    }
}
