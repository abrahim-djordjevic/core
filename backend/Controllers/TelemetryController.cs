using GSInteractiveDeviceAnalyzer.Engine;
using Microsoft.AspNetCore.Mvc;

namespace GSInteractiveDeviceAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class TelemetryController : ControllerBase
    {
        [HttpPost("ram/start")]
        public IActionResult StartRamRadar([FromServices] RamMonitoringEngine ramEngine)
        {
            ramEngine.StartRadar();
            return Ok(new { Message = "RAM Radar Online" });
        }

        [HttpPost("ram/stop")]
        public IActionResult StopRamRadar([FromServices] RamMonitoringEngine ramEngine)
        {
            ramEngine.StopRadar();
            return Ok(new { Message = "RAM Radar Offline" });
        }

        [HttpDelete("ram/kill")]
        public IActionResult KillProcess([FromServices] RamMonitoringEngine ramEngine, [FromBody] List<int> pids)
        {
            if (pids == null || !pids.Any()) return BadRequest(new { Message = "No PIDs provided." });

            var killCount = ramEngine.ExecuteOrder66(pids);
            return Ok(new { Message = $"{killCount} PIDs Terminated" });
        }
    }
}
