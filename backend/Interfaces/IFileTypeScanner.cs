using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface IFileTypeScanner
    {
        FileTypeScanResult? Analyze(string root);
        void Invalidate(string root);
    }
}
