namespace GSInteractiveDeviceAnalyzer.Models;

public class FileTypeExtensionEntry
{
    public string Ext { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public long TotalBytes { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public double PercentOfDisk { get; set; }
}

public class FileTypeCategory
{
    public string Name { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public double PercentOfDisk { get; set; }
    public List<FileTypeExtensionEntry> Extensions { get; set; } = new();
}

public class FileTypeScanResult
{
    public string Root { get; set; } = string.Empty;
    public long TotalScannedBytes { get; set; }
    public string TotalScannedFormatted { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public List<FileTypeCategory> Categories { get; set; } = new();
}