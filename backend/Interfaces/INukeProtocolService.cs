using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface INukeProtocolService
    {
        Task<NukePreviewResponse> PreviewNukeAsync(List<string> paths, CancellationToken cancellationToken = default);
        Task<NukeResultDto> ObliterateNodeAsync(List<string> paths, bool useRecycleBin = false);
        void TriggerNukeAbort();

        // Undo stack operations
        NukeOperation? PeekUndo();
        NukeResultDto? UndoLastNuke();
        List<NukeOperation> GetUndoHistory();
        void ClearUndoStack();
    }
}
