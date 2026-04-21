using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

namespace GSInteractiveDeviceAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController(DiskScannerEngine scanner) : ControllerBase
    {
        private readonly IDiskOperationService _diskService;
        private readonly DiskScannerEngine _scanner = scanner;

        public StorageController(DiskScannerEngine scanner, IDiskOperationService diskService) : this(scanner)
        {
            this._scanner = scanner;
            this._diskService = diskService;
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
        public IActionResult NukeNode([FromQuery] string path)
        {
            try
            {
                var result = _diskService.ObliterateNode(path);

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
                return StatusCode(403, new  ApiResponse<object>
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
    }
}
