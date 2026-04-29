using System.Collections.Concurrent;
using System.Text.Json;
using GSInteractiveDeviceAnalyzer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GSInteractiveDeviceAnalyzer;

public class CacheEntry
{
    public long Size { get; set; }
    public DateTime LastUpdated { get; set; }
}    


public class DiskScannerEngine
{
    public ConcurrentDictionary<string, CacheEntry> DirectorySizeCache = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource _nukeCts;
    private readonly SemaphoreSlim _scanLock = new SemaphoreSlim(1, 1);
    private readonly object _fileWriteLock = new object();
    private readonly string _cacheFilePath = "scanner_memory.json";
    private FileSystemWatcher? _liveRader;
    private readonly object _radarLock = new object();
    private DateTime _lastRadarAlert = DateTime.MinValue;
    private readonly TimeSpan _radarCooldown = TimeSpan.FromMilliseconds(500);

    private readonly IHubContext<StorageHub> _hub;
    private int _scannedFilesCount = 0;

    public DiskScannerEngine(IHubContext<StorageHub> hub)
    {
        _hub = hub;
        if (File.Exists(_cacheFilePath))
        {
            try
            {
                var json = File.ReadAllText(_cacheFilePath);

                var savedMemory = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(json);

                if (savedMemory != null)
                {
                    DirectorySizeCache = new ConcurrentDictionary<string, CacheEntry>(savedMemory);
                    Console.WriteLine($"MEMORY RESTORED: {DirectorySizeCache.Count} folders loaded from disk!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MEMORY CORRUPTED: Starting Fresh. Error: {ex.Message}");
            }
        }
    }

    public List<FileSystemInfo> LoadDirectoryItems(string path)
    {
        MoveRadarToSector(path);

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
        await _scanLock.WaitAsync();
        try
        {
            var directoriesToScan = items.OfType<DirectoryInfo>()
                .Where(d => !DirectorySizeCache.ContainsKey(d.FullName))
                .ToList();

            if (directoriesToScan.Count > 0)
            {
                _scannedFilesCount = 0;
                await _hub.Clients.All.SendAsync("ScanProgress", new { status = "INITIALIZING", count = 0, currentTarget = "Walking up the Engine...." });

                await Parallel.ForEachAsync(directoriesToScan, async (dir, token) =>
                {
                    var size = await Task.Run(() => GetDirectorySize(dir));

                    DirectorySizeCache[dir.FullName] = new CacheEntry
                    {
                        Size = size,
                        LastUpdated = dir.LastWriteTimeUtc
                    };
                });

                SaveMemoryToDisk();
            }
            
            await _hub.Clients.All.SendAsync("ScanProgress", new { status = "COMPLETED", count = _scannedFilesCount, currentTarget = "Scan completed" });
        }
        finally
        {
            _scanLock.Release();
        }
        
    }

    private long GetDirectorySize(DirectoryInfo dir)
    {
        if (DirectorySizeCache.TryGetValue(dir.FullName, out var entry))
        {
            if(dir.LastWriteTimeUtc <= entry.LastUpdated)
                return entry.Size;

            Console.WriteLine("CACHE STALE: Rescanning Directory....");
        }
        long size = 0;
        try
        {
            var option = new EnumerationOptions
            {
                IgnoreInaccessible = true
            };
            var files = dir.GetFiles();
            size += files.Sum(f => f.Length);

            var currentCount = Interlocked.Add(ref _scannedFilesCount, files.Length);

            _ = _hub.Clients.All.SendAsync("ScanProgress", new
            {
                status = "SCANNING",
                count = currentCount,
                currentTarget = dir.Name
            });

            foreach (var subDir in dir.GetDirectories())
            {
                size += GetDirectorySize(subDir);
            }
        }
        catch (Exception e)
        {
        }

        DirectorySizeCache[dir.FullName] = new CacheEntry
        {
            Size = size,
            LastUpdated = dir.LastWriteTimeUtc
        };

        return size;
    }

    public void SaveMemoryToDisk()
    {
        lock (_fileWriteLock)
        {
            try
            {
                string json = JsonSerializer.Serialize(DirectorySizeCache);
                File.WriteAllText(_cacheFilePath, json);
                Console.WriteLine($"MEMORY SAVED: {DirectorySizeCache.Count} folders saved to disk!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR SAVING MEMORY: {ex.Message}");
            }
        }
        
    }

    public void MoveRadarToSector(string targetPath)
    {
        lock (_radarLock)
        {
            try
            {
                if (_liveRader != null)
                {
                    _liveRader.EnableRaisingEvents = false;
                    _liveRader.Dispose();
                    _liveRader = null;
                }

                if (Directory.Exists(targetPath))
                {
                    _liveRader = new FileSystemWatcher(targetPath);

                    _liveRader.IncludeSubdirectories = false;

                    _liveRader.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;

                    _liveRader.Created += OnRadarTriggered;
                    _liveRader.Deleted += OnRadarTriggered;
                    _liveRader.Renamed += OnRadarTriggered;
                    _liveRader.Changed += OnRadarTriggered;

                    _liveRader.EnableRaisingEvents = true;
                    Console.WriteLine($"RADAR ONLINE: Watching Sector -> {targetPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RADAR DEPLOYMENT FAILED: {ex.Message}");
            }
        }
    }

    private void OnRadarTriggered(object sender, FileSystemEventArgs e)
    {
        lock (_radarLock)
        {
            if (DateTime.UtcNow - _lastRadarAlert < _radarCooldown)
            {
                return;
            }

            _lastRadarAlert = DateTime.UtcNow;
        }

        Console.WriteLine($"RADAR ALERT: {e.ChangeType} detected on {e.Name}");

        try
        {
            var folderThatChanged = Path.GetDirectoryName(e.FullPath) ?? "";

            _ = _hub.Clients.All.SendAsync("SectorChanged", folderThatChanged.Replace("\\", "/"));
        }
        catch { }
    }

    public CancellationToken NukeToken()
    {
        _nukeCts?.Cancel();
        _nukeCts = new CancellationTokenSource();
        return _nukeCts.Token;
    }

    public void TriggerAbort()
    {
        _nukeCts?.Cancel();
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