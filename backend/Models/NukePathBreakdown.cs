using System.Collections.Generic;

namespace GSInteractiveDeviceAnalyzer.Models;

public class NukePathBreakdown
{
    public string Path { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int FileCount { get; set; }
}