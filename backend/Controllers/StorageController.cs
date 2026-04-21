using System;
using System.Collections.Generic;
using System.Text;
using GSInteractiveDeviceAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

namespace GSInteractiveDeviceAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController(DiskScannerEngine scanner) : ControllerBase
    {
        private readonly DiskScannerEngine _scanner = scanner;

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

        [HttpPost("nuke")]
        public IActionResult DeleteItem([FromQuery] string path)
        {
            var item = new DirectoryInfo(path).Exists ? (FileSystemInfo)new DirectoryInfo(path) : new FileInfo(path);

            _scanner.ExecuteDelete(item);
            return Ok(new {message = "Item nuked successfully"});
        }
    }
}
