namespace GSInteractiveDeviceAnalyzer.Models;

public class DuplicateGroup
{
    public string FileHash { get; set; } = string.Empty;

    public List<string> FilePaths { get; set; } = new List<string>();
}
