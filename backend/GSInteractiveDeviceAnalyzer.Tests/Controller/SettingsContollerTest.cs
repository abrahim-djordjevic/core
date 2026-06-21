using GSInteractiveDeviceAnalyzer.Controllers;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Models.SettingDtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GSInteractiveDeviceAnalyzer.Tests.Controller
{
    public class SettingsControllerTests
    {
        [Fact]
        public async Task SaveSettings_WhenValidationFails_ReturnsBadRequestAndDoesNotSave()
        {
            var mockService = new Mock<ISettingService>();
            var controller = new SettingsController(mockService.Object);

            var badSettings = AppSettingDto.GetFactoryDefaults();
            badSettings.Advanced.BackendPort = 10; // Illegal port

            var result = await controller.SaveSettings(badSettings);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            mockService.Verify(s => s.SaveAsync(It.IsAny<AppSettingDto>()), Times.Never);
        }

        [Fact]
        public async Task SaveSettings_WhenValidationPasses_ReturnsOkAndCallsSave()
        {
            var mockService = new Mock<ISettingService>();

            var validSettings = AppSettingDto.GetFactoryDefaults();
            mockService.Setup(s => s.Current).Returns(validSettings);

            var controller = new SettingsController(mockService.Object);

            var result = await controller.SaveSettings(validSettings);

            var okResult = Assert.IsType<OkObjectResult>(result);

            mockService.Verify(s => s.SaveAsync(validSettings), Times.Once);
        }

        [Fact]
        public async Task ResetSettings_Always_OverwritesWithFactoryDefaults()
        {
            var mockService = new Mock<ISettingService>();
            var controller = new SettingsController(mockService.Object);

            var result = await controller.ResetSettings();

            Assert.IsType<OkObjectResult>(result);

            mockService.Verify(s => s.SaveAsync(It.IsAny<AppSettingDto>()), Times.Once);
        }
    }
}