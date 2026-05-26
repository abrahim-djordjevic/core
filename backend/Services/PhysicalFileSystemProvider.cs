using System.IO;
using GSInteractiveDeviceAnalyzer.Interfaces;

namespace GSInteractiveDeviceAnalyzer.Services
{
    // Production wrapper that passes commands directly to the real OS
    public class PhysicalFileSystemProvider : IFileSystemProvider
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);
        public bool FileExists(string path) => File.Exists(path);
        public string[] GetDirectories(string path, string searchPattern = "*") => Directory.GetDirectories(path, searchPattern);
        public string[] GetFiles(string path, string searchPattern = "*") => Directory.GetFiles(path, searchPattern);
        public string ReadAllText(string path) => File.ReadAllText(path);
    }
}