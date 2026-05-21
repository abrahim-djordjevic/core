using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface INukeProtocolService
    {
        Task<NukePreviewResponse> PreviewNukeAsync(List<string> paths, CancellationToken cancellationToken = default);
    }
}
