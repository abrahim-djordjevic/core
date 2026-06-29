# Walkthrough: Recycle Bin + Undo Stack (Backend)

## Summary

Implemented the Recycle Bin mode and session-scoped Undo Stack for the Nuke Protocol. When `useRecycleBin` is enabled, files are moved to a staging directory instead of being permanently deleted, and can be restored via the undo endpoint.

## Files Changed

### New Files

| File | Purpose |
|------|---------|
| [NukeExecuteRequest.cs](file:///c:/Users/USER/My%20Project/GSSystemAnalyzer/backend/Models/NukeExecuteRequest.cs) | Request DTO with `Paths` + `UseRecycleBin` flag |
| [NukeOperation.cs](file:///c:/Users/USER/My%20Project/GSSystemAnalyzer/backend/Models/NukeOperation.cs) | Undo stack record (operation ID, timestamps, paths, recycle bin flag) |

### Modified Files

| File | Change |
|------|--------|
| [NukeResultDto.cs](file:///c:/Users/USER/My%20Project/GSSystemAnalyzer/backend/Models/NukeResultDto.cs) | Redesigned from `Message/Path/Type` → `DeletedFiles/FreedBytes/FreedFormatted/SkippedFiles/RecycleBinUsed/Recoverable/OperationId` |
| [INukeProtocolService.cs](file:///c:/Users/USER/My%20Project/GSSystemAnalyzer/backend/Interfaces/INukeProtocolService.cs) | Added `useRecycleBin` param + `PeekUndo()`, `UndoLastNuke()`, `ClearUndoStack()` |
| [NukeProtocolService.cs](file:///c:/Users/USER/My%20Project/GSSystemAnalyzer/backend/Services/NukeProtocolService.cs) | Staging directory logic, undo stack (max 5), file/byte counters, restore logic |
| [NukeController.cs](file:///c:/Users/USER/My%20Project/GSSystemAnalyzer/backend/Controllers/NukeController.cs) | `NukeExecuteRequest` body, 3 new undo endpoints |
| [GSSystemAnalyzer.csproj](file:///c:/Users/USER/My%20Project/GSSystemAnalyzer/backend/GSSystemAnalyzer.csproj) | `Microsoft.WindowsDesktop.App` framework reference (Windows TFM) |

## Design Decisions

### Staging Directory over OS Recycle Bin
Used `%APPDATA%/GSAnalyzer/nuke_trash/{operationId}/` instead of the OS recycle bin because:
- The Windows recycle bin API (`Microsoft.VisualBasic.FileIO`) has no programmatic **restore** — making undo impossible
- The staging directory approach is fully cross-platform
- Files preserve their original path structure for reliable restoration

### Undo Stack
- In-memory `Stack<NukeOperation>` — max 5 entries, LIFO
- Not persisted across restarts (session-scoped by design)
- When stack overflows, oldest entry is evicted and its staging directory is cleaned up
- Undo on permanent-delete operations returns `409 Conflict`

## API Changes

### `DELETE /api/nuke/execute` — Updated Request Body
```diff
- Body: ["path1", "path2"]            // raw List<string>
+ Body: { "paths": [...], "useRecycleBin": true }  // NukeExecuteRequest
```

### New Endpoints
| Method | Route | Status Codes |
|--------|-------|-------------|
| `GET` | `/api/nuke/undo/peek` | 200, 404 |
| `POST` | `/api/nuke/undo` | 200, 404, 409 |
| `DELETE` | `/api/nuke/undo` | 200 |

## Verification
- **Build**: ✅ 0 errors
- **Tests**: ✅ 52/52 passed on both `net10.0` and `net10.0-windows`
