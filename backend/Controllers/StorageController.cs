using GSInteractiveDeviceAnalyzer.Engine;
using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Models;
using GSInteractiveDeviceAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace GSInteractiveDeviceAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly IDiskOperationService _diskService;
        private readonly IDuplicateFileDetector _duplicateFileDetector;

        public StorageController(IDiskOperationService diskService, IDuplicateFileDetector duplicateFileDetector)
        {
            _diskService = diskService;
            _duplicateFileDetector = duplicateFileDetector;
        }

        [HttpPost("stream-sector")]
        public IActionResult StreamDirectorySection([FromServices] IHubContext<SystemHub> hubContext, [FromServices] DiskScannerEngine engine, [FromQuery] string path)
        {
            var cancelToken = engine.ScanToken();

            _ = Task.Run(async () =>
            {
                try
                {
                    var allNodes = _diskService.ScanDirectory(path).ToList();
                    
                    var chunkSize = 100;
                    for (var i = 0; i < allNodes.Count; i += chunkSize)
                    {
                        if (cancelToken.IsCancellationRequested) break;

                        var chunk = allNodes.Skip(i).Take(chunkSize).ToList();
                        await hubContext.Clients.All.SendAsync("DirectoryChunk", new
                        {
                            path = path, chunk = chunk
                        });

                        await Task.Delay(10);
                    }

                    await hubContext.Clients.All.SendAsync("DirectoryStreamComplete", path);
                }
                catch (Exception ex)
                {
                    await hubContext.Clients.All.SendAsync("DirectoryStreamError",
                        new { path = path, error = ex.Message });
                }
                finally
                {
                    await hubContext.Clients.All.SendAsync("DirectoryStreamComplete", path);
                }
            });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Stream Initiated"
            });
        }

        [HttpGet("scan")]
        public async Task<IActionResult> ScanDirectory([FromQuery] string path)
        {
            try
            {
                var result = _diskService.ScanDirectory(path);

                var response = new ApiResponse<IEnumerable<StorageNode>>
                {
                    Success = true,
                    Message = "Directory scanned successfully.",
                    Data = result
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Scan failed: {ex.Message}"
                });
            }


        }

        [HttpGet("drive-stats")]
        public IActionResult GetDriveStats([FromQuery] string driveLetter)
        {
            try
            {
                var stats = _diskService.GetDriveTelemetry(driveLetter);

                var response = new ApiResponse<DriveTelemetryDto>
                {
                    Success = true,
                    Message = "Telemetry retrieved Successfully.",
                    Data = stats
                };


                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Could not read hardware telemetry for drive: " + ex.Message
                });
            }
        }

        [HttpDelete("nuke")]
        public async Task<IActionResult> NukeNode([FromBody] List<string> paths)
        {
            try
            {
                foreach (var path in paths)
                {
                    if (path.StartsWith("C:\\Windows", StringComparison.OrdinalIgnoreCase))
                        return BadRequest(new ApiResponse<object>
                            { Success = false, Message = "CRITICAL OS FILES PROTECTED." });
                }
                var result = await _diskService.ObliterateNode(paths);

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

        [HttpPost("abort-nuke")]
        public IActionResult AbortNuke()
        {
            _diskService.TriggerNukeAbort();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Abort Signal received. Brakes applied."
            });
        }

        [HttpPost("abort-scan")]
        public IActionResult AbortScan()
        {
            _diskService.TriggerScanAbort();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Abort Signal received. Brakes applied."
            });
        }

        [HttpGet("duplicates")]
        public async Task<IActionResult> ScanForDuplicates([FromQuery] string path, [FromServices] DiskScannerEngine engine)
        {
            try
            {
                var cancelToken = engine.ScanToken();
                var duplicateGroups = await _duplicateFileDetector.FindDuplicatesAsync(path);

                return Ok(new
                {
                    success = true,
                    data = duplicateGroups
                });
            }
            catch (OperationCanceledException)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Duplicate Scan Aborted by User."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            { }
        }

        [HttpGet("scan-largefiles")]
        public async Task<IActionResult> GetLargeFiles(
            [FromQuery] string root,
            [FromServices] LargeFileHunterService hunter,
            [FromQuery] int top = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(root) || !System.IO.Directory.Exists(root))
                {
                    return BadRequest( new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Scan failed: Invalid or missing root directory."
                    });
                }

                var result = await hunter.GetTopLargeFilesAsync(root, top);

                var response = new ApiResponse<IEnumerable<LargeFile>>
                {
                    Success = true,
                    Message = "Large files scanned successfully.",
                    Data = result
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Scan failed: {ex.Message}"
                });
            }
        }
    }
}
