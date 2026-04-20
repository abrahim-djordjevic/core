using System.Collections.Concurrent;

namespace GSInteractiveDeviceAnalyzer;

public class DiskScannerEngine
{
    public ConcurrentDictionary<string, long> DirectorySizeCache = new ConcurrentDictionary<string, long>();

    public List<FileSystemInfo> LoadDirectoryItems(string path)
    {
        var items = new List<FileSystemInfo>();
        try
        {
            var dirInfo = new DirectoryInfo(path);
            items.AddRange(dirInfo.GetDirectories().Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System)));
            items.AddRange(dirInfo.GetFiles().Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System)));
        }
        catch (UnauthorizedAccessException )
        {
                
        }

        return items;
    }

    public async Task CalculateMissingSizesAsync(List<FileSystemInfo> items)
    {
        var directoriesToScan = items.OfType<DirectoryInfo>()
            .Where(d => !DirectorySizeCache.ContainsKey(d.FullName))
            .ToList();

        await Parallel.ForEachAsync(directoriesToScan, async (dir, token) =>
        {
            long size = await Task.Run(() => GetDirectorySize(dir));


            DirectorySizeCache.TryAdd(dir.FullName, size);
        });
    }

    private long GetDirectorySize(DirectoryInfo dir)
    {
        long size = 0;
        try
        {
            foreach (var file in dir.GetFiles())
            {
                size += file.Length;
            }

            foreach (var subDir in dir.GetDirectories())
            {
                size += GetDirectorySize(subDir);
            }
        }
        catch (Exception e)
        {
        }

        return size;
    }

    public void ExecuteDelete(FileSystemInfo item)
    {
        if(item.Name == "EMPTY_FOLDER_NO_FILES_HERE") return;

        Console.ResetColor();
        Console.Write($"\n ARE YOU SURE YOU WANT TO DELETE THIS? ");

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(item.Name);
        Console.ResetColor();

        Console.Write("Permanently? (Y/N):  ");
        var confirm = Console.ReadKey(true).Key;
        if (confirm == ConsoleKey.Y)
        {
            try
            {
                if (item is DirectoryInfo dir) dir.Delete(true);
                else if (item is FileInfo file) file.Delete();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error deleting item: {ex.Message}");
                Console.WriteLine("Press Any Key to continue....");
                Console.ReadKey(true);
            }
        }
        
    }
}