using System.Collections.Generic;
using System.Threading;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface IDiskScannerEngine
    {
        void ClearCache();
        CancellationToken NukeToken();
        void TriggerNukeAbort();
        void InvalidatePaths(IEnumerable<string> paths);
    }
}