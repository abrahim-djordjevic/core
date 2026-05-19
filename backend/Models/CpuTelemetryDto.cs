namespace GSInteractiveDeviceAnalyzer.Models
{
    public class CpuTelemetryDto
    {
        public double AverageLoad { get; set; }
        public double Delta { get; set; }
        public Dictionary<string, List<double>> CoreGroups { get; set; } = new();
    }
}
