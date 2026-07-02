using System;
using System.Threading;
using System.Threading.Tasks;
using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Engine
{
    public class DiskScannerEngineTokenTests
    {
        private readonly DiskScannerEngine _engine;

        public DiskScannerEngineTokenTests()
        {
            var hubMock = new Mock<IHubContext<SystemHub>>();
            var settingsMock = new Mock<ISettingService>();
            var loggerMock = new Mock<ILogger<DiskScannerEngine>>();

            _engine = new DiskScannerEngine(hubMock.Object, settingsMock.Object, loggerMock.Object);
        }

        [Fact]
        public void ScanToken_ReturnsLiveToken()
        {
            var token = _engine.ScanToken();
            Assert.False(token.IsCancellationRequested);
        }

        [Fact]
        public void TriggerScanAbort_CancelsToken()
        {
            var token = _engine.ScanToken();
            _engine.TriggerScanAbort();
            Assert.True(token.IsCancellationRequested);
        }

        [Fact]
        public void ScanToken_SuccessiveCalls_CancelPreviousToken()
        {
            var token1 = _engine.ScanToken();
            Assert.False(token1.IsCancellationRequested);

            var token2 = _engine.ScanToken();
            
            Assert.True(token1.IsCancellationRequested);
            Assert.False(token2.IsCancellationRequested);
        }
    }
}
