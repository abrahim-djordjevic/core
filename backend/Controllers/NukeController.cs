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
        [FromBody] NukePreviewRequest request,
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
}
