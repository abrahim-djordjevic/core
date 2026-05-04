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

        [HttpDelete("ram/kill/{pid}")]
        public IActionResult KillProcess([FromServices] RamMonitoringEngine ramEngine, int pid)
        {
            var success = ramEngine.ExecuteOrder66(pid);
            if (success) return Ok(new { Message = $"PID {pid} Terminated" });
            return BadRequest(new { Message = $"Failed to terminate PID {pid}." });
        }
    }
}
