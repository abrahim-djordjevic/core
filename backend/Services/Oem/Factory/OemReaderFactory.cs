using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Services.Oem.Dell;

namespace GSInteractiveDeviceAnalyzer.Services.Oem.Factory
{
    /* public static class OemReaderFactory
    {
        public static IDellOemTelemetry CreateReader()
        {
            var manufacturer = GetSystemManufacturer();

            return manufacturer switch
            {
                "Dell Inc." => new DellOemTelemetry(),
                _ => new UnsupportedOemTelemetry("Unsupported OEM")
            };
        }
    }
    */
}
