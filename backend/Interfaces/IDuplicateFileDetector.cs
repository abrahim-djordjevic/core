using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface IDuplicateFileDetector
    {
        Task<List<DuplicateGroup>> FindDuplicatesAsync(string rootPath, CancellationToken cancellationToken = default);
    }
}
