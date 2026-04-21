using System;
using System.Collections.Generic;
using System.Text;
using GSInteractiveDeviceAnalyzer.Models;
using GSInteractiveDeviceAnalyzer.Services;
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
            var items = _scanner.LoadDirectoryItems(path);
            await _scanner.CalculateMissingSizesAsync(items);

            var response = items.Select(item =>
            {
                DateTime safeDate;
                try
                {
                    safeDate = item.LastWriteTime;
                }
                catch
                {
                    safeDate = DateTime.UtcNow;
                }

                long itemSize = 0;
                if (item is FileInfo f)
                {
                    itemSize = f.Length;
                }
                else if (item is DirectoryInfo d && _scanner.DirectorySizeCache.TryGetValue(d.FullName, out long cachedSize))
                {
                    itemSize = cachedSize;
                }

                return new StorageNode
                {
                    Name = item.Name,
                    Path = item.FullName,
                    Type = item.Attributes.HasFlag(FileAttributes.Directory) ? "Directory" : "File",
                    SizeBytes = itemSize,
                    LastModified = safeDate
                };

            });

            return Ok(response);
        }

        [HttpGet("drive-stats")]
        public IActionResult GetDriveStats([FromQuery] string driveLetter)
        {
            try
            {
                var stats = _diskService.GetDriveTelemetry(driveLetter);


                return Ok(stats);
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Could not read hardware telemetry for drive: " + driveLetter });
            }
        }

        [HttpDelete("nuke")]
        public IActionResult NukeNode([FromQuery] string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    return Ok(new { message = "TARGET NUKED", path = path, type = "File" });
                }
                else if (System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.Delete(path, true);
                    return Ok(new { message = "TARGET NUKED", path = path, type = "Directory" });
                }
                else
                {
                    return NotFound(new { message = "Target not found in the Matrix." });
                }
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { message = "ACCESS DENIED: OS level restricted" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"NUKE FAILED: {ex.Message}" });
            }
        }
    }
}
