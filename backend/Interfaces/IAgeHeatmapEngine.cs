using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Interfaces;

public interface IAgeHeatmapEngine
{
    AgeHeatmapResult? Analyze(string root);
}
