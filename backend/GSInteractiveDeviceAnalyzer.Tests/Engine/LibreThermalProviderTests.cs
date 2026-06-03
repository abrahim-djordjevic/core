using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Services;
#if WINDOWS
using LibreHardwareMonitor.Hardware;
#endif
using Moq;
using Xunit;

namespace GSInteractiveDeviceAnalyzer.Tests.Engine
{
    public class LibreThermalProviderTests
    {
        private readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

#if WINDOWS
        // 🛠️ HELPERS
        private Mock<IHardware> CreateMockHardware(HardwareType type, List<ISensor> sensors)
        {
            var mockHw = new Mock<IHardware>();
            mockHw.Setup(h => h.HardwareType).Returns(type);
            mockHw.Setup(h => h.Sensors).Returns(sensors.ToArray()); // 🚀 FIXED: Added () to ToArray
            mockHw.Setup(h => h.SubHardware).Returns(Array.Empty<IHardware>());
            return mockHw;
        }

        private ISensor CreateMockSensor(SensorType type, string name, float? value)
        {
            var mockSensor = new Mock<ISensor>();
            mockSensor.Setup(s => s.SensorType).Returns(type);
            mockSensor.Setup(s => s.Name).Returns(name);
            mockSensor.Setup(s => s.Value).Returns(value);
            return mockSensor.Object;
        }

        private LibreThermalProvider CreateProvider(
            Mock<IComputerEngine> mockComputer,
            Mock<IWmiThermalFallback>? mockWmi = null,
            Mock<IDellOemFanReader>? mockDell = null)
        {
            var wmi = mockWmi ?? new Mock<IWmiThermalFallback>();
            var dell = mockDell ?? new Mock<IDellOemFanReader>();
            return new LibreThermalProvider(mockComputer.Object, wmi.Object, dell.Object);
        }

        // 🛡️ ORIGINAL CPU & GHOST UI TESTS
        [Fact]
        public async Task GetThermalData_CpuPackageAndCores_MappedCorrectly_ExcludingNulls()
        {
            if (!_isWindows) return;

            var sensors = new List<ISensor>
            {
                CreateMockSensor(SensorType.Temperature, "CPU Package", 68.5f),
                CreateMockSensor(SensorType.Temperature, "CPU Core #1", 65.0f),
                CreateMockSensor(SensorType.Temperature, "CPU Core #2", null),
                CreateMockSensor(SensorType.Temperature, "CPU Core #3", 67.2f),
            };

            var mockCpu = CreateMockHardware(HardwareType.Cpu, sensors);
            var mockComputer = new Mock<IComputerEngine>();
            mockComputer.Setup(c => c.Hardware).Returns(new[] { mockCpu.Object });

            var provider = new LibreThermalProvider(mockComputer.Object);
            var result = await provider.GetThermalDataAsync();

            Assert.Equal(68.5, result.CpuPackageCelsius);
            Assert.Equal(2, result.CoreCelsius.Count);
            Assert.Equal(65.0, result.CoreCelsius[0]);
            Assert.Equal(67.2, result.CoreCelsius[1]);
        }

        [Fact]
        public async Task GetThermalData_WhenAllCoreNull_ReturnsEmptyArrayNotZeros()
        {
            if (!_isWindows) return;

            var sensors = new List<ISensor>
            {
                CreateMockSensor(SensorType.Temperature, "CPU Core #1", null),
                CreateMockSensor(SensorType.Temperature, "CPU Core #2", null),
            };

            var mockCpu = CreateMockHardware(HardwareType.Cpu, sensors);
            var mockComputer = new Mock<IComputerEngine>();
            mockComputer.Setup(c => c.Hardware).Returns(new[] { mockCpu.Object });

            var provider = new LibreThermalProvider(mockComputer.Object);
            var result = await provider.GetThermalDataAsync();

            Assert.NotNull(result.CoreCelsius);
            Assert.Empty(result.CoreCelsius);
        }

        [Fact]
        public async Task GetThermalData_GhostUIVerification_MissingSensorsReturnNull()
        {
            if (!_isWindows) return;

            var mockCpu = CreateMockHardware(HardwareType.Cpu, new List<ISensor>
            {
                CreateMockSensor(SensorType.Temperature, "CPU Package", 45.0f)
            });

            var mockMobo = CreateMockHardware(HardwareType.Motherboard, new List<ISensor>());
            var mockComputer = new Mock<IComputerEngine>();
            mockComputer.Setup(c => c.Hardware).Returns(new[] { mockCpu.Object, mockMobo.Object });

            var provider = new LibreThermalProvider(mockComputer.Object);
            var result = await provider.GetThermalDataAsync();

            Assert.Null(result.CpuPowerWatts);
            Assert.Null(result.MotherboardCelsius);
            Assert.Null(result.ChipsetCelsius);
            Assert.Null(result.NvmeCelsius);
            Assert.Null(result.PumpRpm);
        }

        // 🛡️ EXCEPTION SHIELD & DISPOSE TESTS
        [Fact]
        public async Task GetThermalData_ThermalThrottling_DetectedOnClockDrop()
        {
            var mockSensor = new Mock<ISensor>();
            mockSensor.Setup(s => s.SensorType).Returns(SensorType.Clock);
            mockSensor.Setup(s => s.Name).Returns("CPU Core #0");

            var mockCpu = CreateMockHardware(HardwareType.Cpu, new List<ISensor> { mockSensor.Object });
            var mockComputer = new Mock<IComputerEngine>();
            mockComputer.Setup(c => c.Hardware).Returns(new[] { mockCpu.Object });

            var provider = new LibreThermalProvider(mockComputer.Object);

            mockSensor.Setup(s => s.Value).Returns(4000f);
            var result1 = await provider.GetThermalDataAsync();
            Assert.False(result1.IsThermalThrottling);

            mockSensor.Setup(s => s.Value).Returns(3500f);
            var result2 = await provider.GetThermalDataAsync();
            Assert.False(result2.IsThermalThrottling);

            mockSensor.Setup(s => s.Value).Returns(3000f);
            var result3 = await provider.GetThermalDataAsync();
            Assert.True(result3.IsThermalThrottling);
        }

        [Fact]
        public async Task GetThermalData_WhenHardwareThrows_ReturnsLastGoodPayload()
        {
            var mockComputer = new Mock<IComputerEngine>();
            var provider = new LibreThermalProvider(mockComputer.Object);

            var mockCpu = CreateMockHardware(HardwareType.Cpu, new List<ISensor> {
                CreateMockSensor(SensorType.Temperature, "CPU Package", 70.0f)
            });
            mockComputer.Setup(c => c.Hardware).Returns(new[] { mockCpu.Object });
            var firstResult = await provider.GetThermalDataAsync();
            Assert.Equal(70.0, firstResult.CpuPackageCelsius);

            mockComputer.Setup(c => c.Accept(It.IsAny<IVisitor>())).Throws(new InvalidOperationException("VBS Lockout"));
            var secondResult = await provider.GetThermalDataAsync();

            Assert.NotNull(secondResult);
            Assert.Equal(25.1, secondResult.CpuPackageCelsius);
        }

        [Fact]
        public void Dispose_CallsComputerClose_ExactlyOnce()
        {
            var mockComputer = new Mock<IComputerEngine>();
            var provider = new LibreThermalProvider(mockComputer.Object);

            provider.Dispose();

            mockComputer.Verify(c => c.Close(), Times.Once);
        }

        [Fact]
        public void UpdateVisitor_CallsUpdate_OnTopLevelAndSubHardware()
        {
            var mockSubHw = new Mock<IHardware>();
            var mockTopHw = new Mock<IHardware>();

            mockTopHw.Setup(h => h.SubHardware).Returns(new[] { mockSubHw.Object });
            var visitor = new UpdateVisitor();

            visitor.VisitHardware(mockTopHw.Object);

            mockTopHw.Verify(h => h.Update(), Times.Once);
            mockSubHw.Verify(h => h.Accept(visitor), Times.Once);
        }

        // 🚀 THE FINAL WMI FALLBACK TESTS (The Final 3 Checkboxes!)
        [Fact]
        public void WmiThermalFallback_ConvertsKelvinToCelsius_Correctly()
        {
            // CRITERIA: (currentTemperature / 10.0) − 273.15
            double rawWmiKelvin = 3100; // 3100 = 310.0K = 36.85°C. Math.Round(36.85, 1) = 36.9°C

            var result = WmiThermalFallback.ConvertKelvinToCelsius(rawWmiKelvin);

            Assert.Equal(36.9, result);
        }

        [Fact]
        public async Task GetThermalData_WhenLhmIsNull_UsesAcpiFallbackValue()
        {
            // CRITERIA: cpuPackageCelsius uses ACPI fallback value when LHM CPU Package sensor is null
            var mockComputer = new Mock<IComputerEngine>();
            var mockCpu = CreateMockHardware(HardwareType.Cpu, new List<ISensor>()); // NO sensors
            mockComputer.Setup(c => c.Hardware).Returns(new[] { mockCpu.Object });

            var mockWmi = new Mock<IWmiThermalFallback>();
            mockWmi.Setup(w => w.GetCpuTemperatureCelsius()).Returns(45.5); // Mock WMI reads 45.5

            var provider = new LibreThermalProvider(mockComputer.Object, mockWmi.Object);
            var result = await provider.GetThermalDataAsync();

            Assert.Equal(45.5, result.CpuPackageCelsius); // Proves the fallback was triggered
            mockWmi.Verify(w => w.GetCpuTemperatureCelsius(), Times.Once);
        }

        [Fact]
        public async Task GetThermalData_WhenWmiReturnsNoInstances_ReturnsNull_DoesNotThrow()
        {
            // CRITERIA: When WMI ACPI returns no instances, fallback returns null — does not throw
            var mockComputer = new Mock<IComputerEngine>();
            var mockCpu = CreateMockHardware(HardwareType.Cpu, new List<ISensor>());
            mockComputer.Setup(c => c.Hardware).Returns(new[] { mockCpu.Object });

            var mockWmi = new Mock<IWmiThermalFallback>();
            mockWmi.Setup(w => w.GetCpuTemperatureCelsius()).Returns((double?)null); // WMI fails/locked

            var provider = new LibreThermalProvider(mockComputer.Object, mockWmi.Object);

            var exception = await Record.ExceptionAsync(() => provider.GetThermalDataAsync());
            var result = await provider.GetThermalDataAsync();

            Assert.Null(exception); // Did not throw a crash!
            Assert.Null(result.CpuPackageCelsius); // Safely degraded to null for the Ghost UI
        }

        [Fact]
        public void DellOemFanReader_CalculateRpm_AppliesUnitModifierCorrectly()
        {
            // 1. ARRANGE
            long rawReading = 35;
            int unitModifier = 2; // 10^2 = 100. (35 * 100 = 3500 RPM)

            // 2. ACT
            var result = DellOemFanReader.CalculateRpm(rawReading, unitModifier);

            // 3. ASSERT
            Assert.Equal(3500, result);
        }

        [Fact]
        public async Task GetThermalData_WhenLhmFanIsZero_UsesDellOemFanFallback()
        {
            // 1. ARRANGE: LHM returns NO fans
            var mockComputer = new Mock<IComputerEngine>();
            var mockCpu = CreateMockHardware(HardwareType.Cpu, new List<ISensor>());
            mockComputer.Setup(c => c.Hardware).Returns(new[] { mockCpu.Object });

            // Mock Dell returning 3800 RPM
            var mockDell = new Mock<IDellOemFanReader>();
            mockDell.Setup(d => d.TryGetDellOemFans()).Returns(new DellFanReading { CpuFanRpm = 3800 });

            var provider = CreateProvider(mockComputer, null, mockDell);

            // 2. ACT
            var result = await provider.GetThermalDataAsync();

            // 3. ASSERT
            Assert.Equal(3800, result.CpuFanRpm);
            mockDell.Verify(d => d.TryGetDellOemFans(), Times.Once);
        }

        [Fact]
        public async Task GetThermalData_WhenDellOemFails_UsesLastGoodPayloadFanCache()
        {
            // 1. ARRANGE
            var mockComputer = new Mock<IComputerEngine>();

            var superIoSensors = new List<ISensor> {
                CreateMockSensor(SensorType.Fan, "CPU Fan", 3000f)
            };
            var mockSuperIo = CreateMockHardware(HardwareType.SuperIO, superIoSensors);

            // Tick 1: Motherboard containing the SuperIO chip (Success)
            var mockMoboSuccess = new Mock<IHardware>();
            mockMoboSuccess.Setup(h => h.HardwareType).Returns(HardwareType.Motherboard);
            mockMoboSuccess.Setup(h => h.SubHardware).Returns(new[] { mockSuperIo.Object });
            mockMoboSuccess.Setup(h => h.Sensors).Returns(Array.Empty<ISensor>());

            // Tick 2: Empty Motherboard (Fails to read fans)
            var mockMoboFail = new Mock<IHardware>();
            mockMoboFail.Setup(h => h.HardwareType).Returns(HardwareType.Motherboard);
            mockMoboFail.Setup(h => h.SubHardware).Returns(Array.Empty<IHardware>());
            mockMoboFail.Setup(h => h.Sensors).Returns(Array.Empty<ISensor>());

            // Sequence: Tick 1 succeeds, Tick 2 fails
            mockComputer.SetupSequence(c => c.Hardware)
                        .Returns(new[] { mockMoboSuccess.Object })
                        .Returns(new[] { mockMoboFail.Object });

            // Dell OEM Bridge is dead/returns null
            var mockDell = new Mock<IDellOemFanReader>();
            mockDell.Setup(d => d.TryGetDellOemFans()).Returns((DellFanReading?)null);

            var provider = CreateProvider(mockComputer, null, mockDell);

            // 2. ACT
            await provider.GetThermalDataAsync(); // Tick 1 successfully caches 3000
            var secondResult = await provider.GetThermalDataAsync(); // Tick 2 fails LHM, fails Dell, uses cache!

            // 3. ASSERT
            Assert.Equal(3000, secondResult.CpuFanRpm); // Proves the Exception Shield holds!
        }
#endif

        //  PLATFORM SUPPORT TEST
        [Fact]
        public async Task LibreThermalProvider_NonWindowsOS_ThrowsPlatformNotSupportedException()
        {
#if WINDOWS
            return;
#else
            var provider = new LibreThermalProvider();
            var exception = await Record.ExceptionAsync(() => provider.GetThermalDataAsync());
            Assert.NotNull(exception);
            Assert.IsType<PlatformNotSupportedException>(exception);
#endif
        }
    }
}