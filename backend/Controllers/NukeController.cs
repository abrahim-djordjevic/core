using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GSInteractiveDeviceAnalyzer.Models;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Services;

namespace GSInteractiveDeviceAnalyzer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NukeController : ControllerBase
{
    private readonly INukeProtocolService _nukeService;

    public NukeController (INukeProtocolService nukeService)
    {
        _nukeService = nukeService;
    }

    [HttpPost("preview")]
    public async Task<IActionResult> GetNukePreview(
        [FromBody] NukePreviewRequest request, [FromServices] IDriveDetectionService driveService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request == null || request.Paths == null || request.Paths.Count == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Preview failed: No target paths were specified."
                });
            }

            var readyDrives = driveService.GetReadyDrives();
            foreach (var path in request.Paths)
            {
                var normalizedRoot = Path.GetPathRoot(path)?.ToUpperInvariant() ?? path.ToUpperInvariant();
                if (!readyDrives.Any(driveService => driveService.Name.ToUpperInvariant() == normalizedRoot))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Drive not ready or not found: {normalizedRoot}"
                    });
                }
            }

            var previewData = await _nukeService.PreviewNukeAsync(request.Paths, cancellationToken);

            var response = new ApiResponse<NukePreviewResponse>
            {
                Success = true,
                Message = "Dry run preview generated successfully.",
                Data = previewData
            };

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, new ApiResponse<object>
            {
                Success = false,
                Message = "Preview generation aborted by client."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"An internal server error occured: {ex.Message}"
            });
        }
    }

    [HttpDelete("execute")]
    public async Task<IActionResult> NukeNode([FromBody] NukeExecuteRequest request, [FromServices] IDriveDetectionService driveService)
    {
        try
        {
            if (request == null || request.Paths == null || request.Paths.Count == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Nuke failed: No target paths were specified."
                });
            }

            var readyDrives = driveService.GetReadyDrives();

            foreach (var path in request.Paths)
            {
                var normalizedRoot = Path.GetPathRoot(path)?.ToUpperInvariant() ?? path.ToUpperInvariant();
                if (!readyDrives.Any(d => d.Name.ToUpperInvariant() == normalizedRoot))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Drive not ready or not found: {normalizedRoot}"
                    });
                }


                if (path.StartsWith("C:\\Windows", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new ApiResponse<object>
                        { Success = false, Message = "CRITICAL OS FILES PROTECTED." });
            }
            var result = await _nukeService.ObliterateNodeAsync(request.Paths, request.UseRecycleBin);

            var response = new ApiResponse<NukeResultDto>
            {
                Success = true,
                Message = "Target Nuked Successfully",
                Data = result
            };

            return Ok(response);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = "ACCESS DENIED: OS level restricted"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Could not nuke target " + ex.Message
            });
        }
    }

    [HttpPost("abort")]
    public IActionResult AbortNuke()
    {
        _nukeService.TriggerNukeAbort();

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Abort Signal received. Brakes applied."
        });
    }

    // ──────────────────────────────────────────────
    //  Undo Endpoints
    // ──────────────────────────────────────────────

    [HttpGet("undo/peek")]
    public IActionResult PeekUndo()
    {
        var operation = _nukeService.PeekUndo();

        if (operation is null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "No undoable operations in the stack."
            });
        }

        return Ok(new ApiResponse<NukeOperation>
        {
            Success = true,
            Message = "Most recent undoable operation.",
            Data = operation
        });
    }

    [HttpGet("undo/history")]
    public IActionResult GetUndoHistory()
    {
        var history = _nukeService.GetUndoHistory();

        return Ok(new ApiResponse<IEnumerable<NukeOperation>>
        {
            Success = true,
            Message = "Undo history retrieved.",
            Data = history
        });
    }

    [HttpPost("undo")]
    public IActionResult UndoLastNuke()
    {
        var peeked = _nukeService.PeekUndo();

        if (peeked is null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "No undoable operations in the stack."
            });
        }

        if (!peeked.UsedRecycleBin)
        {
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Message = "Cannot undo: files were permanently deleted.",
                Data = new { error = "PERMANENT_DELETE" }
            });
        }

        var result = _nukeService.UndoLastNuke();

        return Ok(new ApiResponse<NukeResultDto>
        {
            Success = true,
            Message = "Undo completed — files restored to original locations.",
            Data = result
        });
    }

    [HttpDelete("undo")]
    public IActionResult ClearUndoStack()
    {
        _nukeService.ClearUndoStack();

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Undo stack cleared."
        });
    }
}
