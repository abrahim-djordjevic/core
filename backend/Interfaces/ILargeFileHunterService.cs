using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface ILargeFileHunterService
    {
        Task<List<LargeFile>> GetTopLargeFilesAsync(string rootPath, int topN, CancellationToken cancellationToken = default);
    }
}