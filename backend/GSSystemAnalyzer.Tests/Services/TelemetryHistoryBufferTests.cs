using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Services;

namespace GSSystemAnalyzer.Tests.Services
{
    public class TelemetryHistoryBufferTests
    {
        private readonly ITelemetryHistoryBuffer _buffer = new TelemetryHistoryBuffer();

        [Fact]
        public void Record_AddsPointToBuffer()
        {
            _buffer.Record("cpu", 42.5);

            var result = _buffer.GetHistory("cpu", 5);

            Assert.NotNull(result);
            Assert.Single(result!.Points);
            Assert.Equal(42.5, result.Points[0].Value);
        }

        [Fact]
        public void GetHistory_ReturnsCorrectWindow_5Min()
        {
            var buffer = new TelemetryHistoryBuffer();

            // Use reflection-free approach: record points, then query
            // Points recorded now should appear in a 5-minute window
            buffer.Record("cpu", 10.0);
            buffer.Record("cpu", 20.0);
            buffer.Record("cpu", 30.0);

            var result = buffer.GetHistory("cpu", 5);

            Assert.NotNull(result);
            Assert.Equal(3, result!.Points.Count);
            Assert.Equal(5, result.Minutes);
        }

        [Fact]
        public void GetHistory_ReturnsCorrectWindow_60Min()
        {
            var buffer = new TelemetryHistoryBuffer();

            buffer.Record("cpu", 55.0);

            var result = buffer.GetHistory("cpu", 60);

            Assert.NotNull(result);
            Assert.Single(result!.Points);
            Assert.Equal(60, result.Minutes);
        }

        [Fact]
        public void GetHistory_ComputesStatsCorrectly()
        {
            _buffer.Record("cpu", 10.0);
            _buffer.Record("cpu", 50.0);
            _buffer.Record("cpu", 30.0);
            _buffer.Record("cpu", 90.0);

            var result = _buffer.GetHistory("cpu", 5);

            Assert.NotNull(result);
            Assert.Equal(10.0, result!.Stats.Min);
            Assert.Equal(90.0, result.Stats.Max);
            Assert.Equal(45.0, result.Stats.Avg);
            Assert.Equal(90.0, result.Stats.Current); // Last recorded point
        }

        [Fact]
        public void GetHistory_ReturnsNullForUnknownMetric()
        {
            var result = _buffer.GetHistory("does_not_exist", 5);

            Assert.Null(result);
        }

        [Fact]
        public void GetHistory_ReturnsEmptyPointsWhenNoData()
        {
            // "cpu" is a valid metric but we haven't recorded anything
            var buffer = new TelemetryHistoryBuffer();
            var result = buffer.GetHistory("cpu", 5);

            Assert.NotNull(result);
            Assert.Empty(result!.Points);
            Assert.Equal(0, result.Stats.Min);
            Assert.Equal(0, result.Stats.Max);
            Assert.Equal(0, result.Stats.Avg);
            Assert.Equal(0, result.Stats.Current);
        }

        [Fact]
        public void GetSupportedMetrics_ReturnsAllRegisteredKeys()
        {
            var metrics = _buffer.GetSupportedMetrics();

            Assert.Contains("cpu", metrics);
            Assert.Contains("ram", metrics);
            Assert.Contains("ram_percent", metrics);
            Assert.Contains("thermal_cpu_package", metrics);
            Assert.Contains("network_rx", metrics);
            Assert.Contains("network_tx", metrics);
            Assert.Contains("disk_io_read", metrics);
            Assert.Contains("disk_io_write", metrics);
            Assert.Equal(8, metrics.Count);
        }

        [Fact]
        public void Record_IgnoresUnknownMetric()
        {
            // Should not throw, just silently ignored
            _buffer.Record("unknown_metric", 99.0);

            var result = _buffer.GetHistory("unknown_metric", 5);
            Assert.Null(result);
        }

        [Fact]
        public void Record_BothRamMetrics_StoresSeparately()
        {
            _buffer.Record("ram", 8.5);
            _buffer.Record("ram_percent", 53.1);

            var ramResult = _buffer.GetHistory("ram", 5);
            var pctResult = _buffer.GetHistory("ram_percent", 5);

            Assert.NotNull(ramResult);
            Assert.NotNull(pctResult);
            Assert.Single(ramResult!.Points);
            Assert.Single(pctResult!.Points);
            Assert.Equal(8.5, ramResult.Points[0].Value);
            Assert.Equal(53.1, pctResult.Points[0].Value);
            Assert.Equal("GB", ramResult.Unit);
            Assert.Equal("%", pctResult.Unit);
        }

        [Fact]
        public void GetHistory_RoundsValuesToTwoDecimals()
        {
            _buffer.Record("cpu", 42.5678);

            var result = _buffer.GetHistory("cpu", 5);

            Assert.NotNull(result);
            Assert.Equal(42.57, result!.Points[0].Value);
        }

        [Fact]
        public void GetHistory_ClampsMinutesToValidRange()
        {
            _buffer.Record("cpu", 50.0);

            var tooLow = _buffer.GetHistory("cpu", -10);
            var tooHigh = _buffer.GetHistory("cpu", 200);

            Assert.NotNull(tooLow);
            Assert.Equal(1, tooLow!.Minutes);

            Assert.NotNull(tooHigh);
            Assert.Equal(60, tooHigh!.Minutes);
        }

        [Fact]
        public void GetHistory_PointsAreOrderedByTimestamp()
        {
            // Record multiple points rapidly
            for (int i = 0; i < 10; i++)
            {
                _buffer.Record("cpu", i * 10.0);
            }

            var result = _buffer.GetHistory("cpu", 5);

            Assert.NotNull(result);

            for (int i = 1; i < result!.Points.Count; i++)
            {
                Assert.True(result.Points[i].Timestamp >= result.Points[i - 1].Timestamp);
            }
        }

        [Fact]
        public void ConcurrentAccess_DoesNotThrow()
        {
            var buffer = new TelemetryHistoryBuffer();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Parallel writes
            var writeTask = Task.Run(() =>
            {
                Parallel.For(0, 1000, i =>
                {
                    try
                    {
                        buffer.Record("cpu", i % 100);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            });

            // Parallel reads
            var readTask = Task.Run(() =>
            {
                Parallel.For(0, 500, _ =>
                {
                    try
                    {
                        buffer.GetHistory("cpu", 5);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            });

            Task.WaitAll(writeTask, readTask);

            Assert.Empty(exceptions);
        }
    }
}
