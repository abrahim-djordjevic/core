using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GSSystemAnalyzer.Controllers;
using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Controller
{
    public class StorageControllerCancellationTests
    {
        [Fact]
        public async Task ScanForDuplicates_WhenCanceled_Returns499StatusCode()
        {
            // Arrange
            var mockDiskService = new Mock<IDiskOperationService>();
            var mockDuplicateFileDetector = new Mock<IDuplicateFileDetector>();
            var mockDriveService = new Mock<IDriveDetectionService>();

            string tempPath = Path.GetTempPath();
            string rootDrive = Path.GetPathRoot(tempPath);

            // Mock ready drive so we pass validation
            mockDriveService.Setup(d => d.GetReadyDrives())
                .Returns(new System.Collections.Generic.List<DriveMetric> { new DriveMetric { Name = rootDrive } });

            // Make duplicate detector throw OperationCanceledException
            mockDuplicateFileDetector
                .Setup(d => d.FindDuplicatesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var hubMock = new Mock<IHubContext<SystemHub>>();
            var settingsMock = new Mock<ISettingService>();
            var loggerMock = new Mock<ILogger<DiskScannerEngine>>();
            var engine = new DiskScannerEngine(hubMock.Object, settingsMock.Object, loggerMock.Object);

            var controller = new StorageController(mockDiskService.Object, mockDuplicateFileDetector.Object);

            var request = new ScanRequest { Root = tempPath };

            // Act
            var result = await controller.ScanForDuplicates(request, engine, mockDriveService.Object);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(499, objectResult.StatusCode);

            var response = Assert.IsType<ApiResponse<object>>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Duplicate Scan Aborted by User.", response.Message);
        }
    }
}
