using System.Collections.Generic;

namespace GSInteractiveDeviceAnalyzer.Models;

public class NukeExecuteRequest
{
    public List<string> Paths { get; set; } = new();
    public string PlanToken { get; set; } = string.Empty;
    public bool UseRecycleBin { get; set; } = false;
}
