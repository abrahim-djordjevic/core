using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GSInteractiveDeviceAnalyzer.Models;
using GSInteractiveDeviceAnalyzer.Services;
using GSInteractiveDeviceAnalyzer.Interfaces;

namespace GSInteractiveDeviceAnalyzer.Tests.Fakes;

public class FakeLargeFileHunter : ILargeFileHunterService
{
    public Task<List<LargeFile>> GetTopLargeFilesAsync(string rootPath, int topN, CancellationToken token = default)
    {
        return Task.FromResult(new List<LargeFile>());
    }
}