using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces
{
    public interface IDuplicateFileDetector
    {
        Task<List<DuplicateGroup>> FindDuplicatesAsync(string rootPath, CancellationToken cancellationToken = default);
    }
}
