using System.Collections.Generic;

namespace GSInteractiveDeviceAnalyzer.Models;

public class NukeExecuteRequest
{
    public List<string> Paths { get; set; } = new();
    public bool UseRecycleBin { get; set; } = false;
}
