using System.Collections.Generic;

namespace GSInteractiveDeviceAnalyzer.Models;

public class NukePreviewRequest
{
    public List<string> Paths { get; set; } = new();
}