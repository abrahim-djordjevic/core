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

        [HttpPost("scan")]
        public async Task<IActionResult> ScanDirectory(
            [FromBody] ScanRequest request, 
            [FromServices] IDriveDetectionService driveService)
        {
            try
            {
                string targetPath = string.IsNullOrWhiteSpace(request?.Root)
                    ? (Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\")
                    : request.Root;
                
                var normalizedRoot = Path.GetPathRoot(targetPath)?.ToUpperInvariant() ?? targetPath.ToUpperInvariant();

                var readyDrives = driveService.GetReadyDrives();
                if (!readyDrives.Any(d => d.Name.ToUpperInvariant() == normalizedRoot))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Drive not ready or found: {normalizedRoot}"
                    });
                }

                if (!System.IO.Directory.Exists(targetPath))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Scan failed: Invalid or missing target directory."
                    });
                }

                var result = await Task.Run(() => _diskService.ScanDirectory(targetPath));

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

        [HttpPost("duplicates")]
        public async Task<IActionResult> ScanForDuplicates(
            [FromBody] ScanRequest request,
            [FromServices] DiskScannerEngine engine,
            [FromServices] IDriveDetectionService driveService)
        {
            try
            {
                string targetPath = string.IsNullOrWhiteSpace(request?.Root)
                    ? (Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\")
                    : request.Root;
                
                var normalizedRoot = Path.GetPathRoot(targetPath)?.ToUpperInvariant() ?? targetPath.ToUpperInvariant();

                var readyDrives = driveService.GetReadyDrives();
                if (!readyDrives.Any(d => d.Name.ToUpperInvariant() == normalizedRoot))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Drive not ready or not found: {normalizedRoot}"
                    });
                }

                if (!System.IO.Directory.Exists(targetPath))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Scan failed: Invalid or missing target directory."
                    });
                }

                var cancelToken = engine.ScanToken();
                var duplicateGroups = await _duplicateFileDetector.FindDuplicatesAsync(targetPath);

                return Ok(new ApiResponse<IEnumerable<DuplicateGroup>>
                {
                    Success = true,
                    Message = "Duplicates scanned successfully.",
                    Data = duplicateGroups
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, new ApiResponse<object>
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
                    Message = $"Scan failed: {ex.Message}"
                });
            }
        }

        [HttpGet("scan-largefiles")]
        public async Task<IActionResult> GetLargeFiles(
            [FromServices] ILargeFileHunterService hunter,
            [FromServices] IDriveDetectionService driveService,
            CancellationToken cancellationToken,
            [FromQuery] string? root = null,
            [FromQuery] int top = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(root))
                {
                    root = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                }

                var normalizedRoot = Path.GetPathRoot(root)?.ToUpperInvariant() ?? root.ToUpperInvariant();

                var readyDrives = driveService.GetReadyDrives();
                if (!readyDrives.Any(d => d.Name.ToUpperInvariant() == normalizedRoot))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Drive not ready or not found: {normalizedRoot}"
                    });
                }

                if (!System.IO.Directory.Exists(root))
                {
                    return BadRequest( new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Scan failed: Invalid or missing root directory."
                    });
                }

                var result = await hunter.GetTopLargeFilesAsync(root, top, cancellationToken);

                var response = new ApiResponse<IEnumerable<LargeFile>>
                {
                    Success = true,
                    Message = "Large files scanned successfully.",
                    Data = result
                };

                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Client closed request, Scan Aborted"
                });
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
