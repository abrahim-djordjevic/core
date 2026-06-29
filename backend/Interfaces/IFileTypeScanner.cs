using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces
{
    public interface IFileTypeScanner
    {
        FileTypeScanResult? Analyze(string root);
        void Invalidate(string root);
    }
}
