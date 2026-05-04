namespace GSInteractiveDeviceAnalyzer.Models
{
    public class ProcessTelemetry
    {
        public int ProcessId { get; set; }
        public string Name { get; set; }
        public long WorkingSetBytes { get; set; }
        public double RamMb => Math.Round(WorkingSetBytes / 102.0 / 1024.0, 2);
    }
}
