using System.Collections.Concurrent;
using System.Text.Json;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Engine;

public class CacheEntry
{
    public long Size { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, FileTypeEntry>? Extensions { get; set; }
}    


public class DiskScannerEngine : IDiskScannerEngine
{
    public ConcurrentDictionary<string, CacheEntry> DirectorySizeCache = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _nukeCts;
    private CancellationTokenSource? _scanCts;
    private readonly SemaphoreSlim _scanLock = new SemaphoreSlim(1, 1);
    private readonly object _fileWriteLock = new object();
    private int _deepScanThrottle = 0;
    private readonly string _cacheFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GSAnalyzer", "scanner_memory.json");
    private FileSystemWatcher? _liveRader;
    private readonly object _radarLock = new object();
    private DateTime _lastRadarAlert = DateTime.MinValue;
    private readonly TimeSpan _radarCooldown = TimeSpan.FromMilliseconds(500);
    private readonly ISettingService _settings;
    private readonly IHubContext<SystemHub> _hub;
    private readonly ILogger<DiskScannerEngine> _logger;
    private int _scannedFilesCount = 0;

    public DiskScannerEngine(IHubContext<SystemHub> hub, ISettingService settings, ILogger<DiskScannerEngine> logger)
    {
        _hub = hub;
        _settings = settings;
        _logger = logger;
        if (File.Exists(_cacheFilePath))
        {
            try
            {
                var json = File.ReadAllText(_cacheFilePath);

                var savedMemory = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(json);

                if (savedMemory != null)
                {
                    DirectorySizeCache = new ConcurrentDictionary<string, CacheEntry>(savedMemory);
                    _logger.LogInformation("Cache restored: {Count} folders loaded from disk", DirectorySizeCache.Count);

                    PruneStaleCacheEntries();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache file corrupted, starting fresh");
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

            var totalNodes = directoriesToScan.Count;

            if (totalNodes > 0)
            {
                var completedNode = 0;
                _deepScanThrottle = 0;
                _scannedFilesCount = 0;

                var token = _scanCts?.Token ?? CancellationToken.None;

                await _hub.Clients.All.SendAsync("ScanProgress", new { status = "INITIALIZING", count = 0, currentTarget = "Walking up the Engine...." });

                await Parallel.ForEachAsync(directoriesToScan, async (dir, token) =>
                {
                    if (token.IsCancellationRequested) return;
                    try
                    {
                        var size = await Task.Run(() => GetDirectorySize(dir, token), token);

                        DirectorySizeCache.TryGetValue(dir.FullName, out var existingEntry);
                        DirectorySizeCache[dir.FullName] = new CacheEntry
                        {
                            Size = size,
                            LastUpdated = dir.LastWriteTimeUtc,
                            Extensions = existingEntry?.Extensions
                        };
                    }
                    catch (OperationCanceledException )
                    {
                        return;
                    }
                    
                    var completed = Interlocked.Increment(ref completedNode);
                    var percentage = Math.Round(((double)completed / totalNodes) * 100, 1);

                    _ = _hub.Clients.All.SendAsync("ScanProgress", new
                    {
                        completed = completed,
                        total = totalNodes,
                        percentageComplete = percentage,
                        currentTarget = dir.Name
                    });
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

    private long GetDirectorySize(DirectoryInfo dir, CancellationToken token, int currentDepth = 1)
    {
        if (token.IsCancellationRequested)
            throw new OperationCanceledException("Scan aborted by user");

        var config = _settings.Current.Scan;

        if (currentDepth > config.Depth) return 0;

        var normalizedPath = dir.FullName.Replace("\\", "/");
        if (config.ExcludedPaths.Any(p =>
                normalizedPath.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return 0;

        if (DirectorySizeCache.TryGetValue(dir.FullName, out var entry))
        {
            if (dir.LastWriteTimeUtc <= entry.LastUpdated)
                return entry.Size;

            _logger.LogDebug("Cache stale for directory, rescanning");
        }

        long size = 0;
        var extMap = new Dictionary<string, FileTypeEntry>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var option = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                AttributesToSkip = 0
            };

            if (config.SkipHiddenFiles) option.AttributesToSkip |= FileAttributes.Hidden;
            if (config.SkipSystemFiles) option.AttributesToSkip |= FileAttributes.System;

            if (!config.FollowSymlinks &&
                dir.Attributes.HasFlag(FileAttributes.ReparsePoint))
                return 0;

            var files = dir.GetFiles("*", option);
            size += files.Sum(f => f.Length);

            foreach (var f in files)
            {
                var ext = f.Extension.ToLowerInvariant();
                if (string.IsNullOrEmpty(ext)) ext = "no extension";

                if (!extMap.TryGetValue(ext, out var fte))
                {
                    fte = new FileTypeEntry { Count = 0, Bytes = 0, LargestFileBytes = 0, LargestFilePath = string.Empty };
                    extMap[ext] = fte;
                }
                fte.Count++;
                fte.Bytes += f.Length;
                
                if (f.Length > fte.LargestFileBytes)
                {
                    fte.LargestFileBytes = f.Length;
                    fte.LargestFilePath = f.FullName;
                }
            }

            var pulse = Interlocked.Increment(ref _deepScanThrottle);
            var currentCount = Interlocked.Add(ref _scannedFilesCount, files.Length);

            if (pulse % 50 == 0)
            {
                _ = _hub.Clients.All.SendAsync("ScanProgress", new
                {
                    status = "SCANNING",
                    count = currentCount,
                    currentTarget = dir.Name
                });
            }

            foreach (var subDir in dir.GetDirectories("*", option))
            {
                // Pass depth + 1 so each recursive level is tracked
                size += GetDirectorySize(subDir, token, currentDepth + 1);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { /* access denied etc — skip silently */ }

        DirectorySizeCache[dir.FullName] = new CacheEntry
        {
            Size = size,
            LastUpdated = dir.LastWriteTimeUtc,
            Extensions = extMap
        };

        return size;
    }

    private long ScanDirectoryR(DirectoryInfo dir, CancellationToken token, int currentDepth = 1)
    {
        token.ThrowIfCancellationRequested();

        var config = _settings.Current.Scan;

        if (currentDepth > config.Depth) return 0;

        var normalizedPath = dir.FullName.Replace("\\", "/");
        if (config.ExcludedPaths.Any(p => normalizedPath.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            return 0;
        }

        long size = 0;

        var option = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = 0
        };

        if (config.SkipHiddenFiles) option.AttributesToSkip |= FileAttributes.Hidden;
        if (config.SkipSystemFiles) option.AttributesToSkip |= FileAttributes.System;

        try
        {
            foreach (var file in dir.EnumerateFiles("*", option))
            {
                size += file.Length;
                Interlocked.Increment(ref _scannedFilesCount);
            }

            foreach (var subDir in dir.EnumerateDirectories("*", option))
            {
                size += ScanDirectoryR(subDir, token, currentDepth + 1);
            }
        }
        catch (UnauthorizedAccessException )
        {
        }

        return size;
    }

    public void SaveMemoryToDisk()
    {
        lock (_fileWriteLock)
        {
            try
            {
                // Existing — save directory sizes
                var dirJson = JsonSerializer.Serialize(
                    new Dictionary<string, CacheEntry>(DirectorySizeCache));
                Directory.CreateDirectory(Path.GetDirectoryName(_cacheFilePath)!);
                File.WriteAllText(_cacheFilePath, dirJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save cache to disk");
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
                    _logger.LogInformation("File system watcher active on {Path}", targetPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deploy file system watcher on {Path}", targetPath);
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

        _logger.LogDebug("File system change detected: {ChangeType} on {Name}", e.ChangeType, e.Name);

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

    public void TriggerNukeAbort()
    {
        _nukeCts?.Cancel();
    }

    public CancellationToken ScanToken()
    {
        _scanCts?.Cancel();
        _scanCts  = new CancellationTokenSource();
        return _scanCts.Token;
    }

    public void TriggerScanAbort()
    {
        _scanCts?.Cancel();
        _logger.LogInformation("Scan abort signal received");
    }

    public void PruneStaleCacheEntries()
    {
        var config = _settings.Current.Cache;
        var cutoff = DateTime.UtcNow.AddMinutes(-config.ScanCacheTtlMinutes);

        // Remove entries that are past their TTL
        var stale = DirectorySizeCache
            .Where(kvp => kvp.Value.LastUpdated < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in stale)
            DirectorySizeCache.TryRemove(key, out _);

        _logger.LogDebug("Cache pruned: {Count} stale entries removed (TTL = {TtlMinutes} min)", stale.Count, config.ScanCacheTtlMinutes);
    }

    public void ClearCache()
    {
        DirectorySizeCache.Clear();

        lock (_fileWriteLock)
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                    File.Delete(_cacheFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete cache file");
            }
        }

        _logger.LogInformation("Cache cleared — memory wiped, scanner_memory.json deleted");
    }
    public void ExecuteDelete(FileSystemInfo item)
    {
        if(item.Name == "EMPTY_FOLDER_NO_FILES_HERE") return;

        try
        {
            if (item is DirectoryInfo dir) dir.Delete(true);
            else if (item is FileInfo file) file.Delete();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete item");
        }
    }

    public void InvalidatePaths(IEnumerable<string> paths)
    {
        var nukedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parentsToRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var subtreePrefixes = new List<string>();

        foreach (var path in paths)
        {
            var normalizedPath = Path.GetFullPath(path);
            nukedSet.Add(normalizedPath);

            subtreePrefixes.Add(
                normalizedPath.EndsWith(Path.DirectorySeparatorChar.ToString())
                    ? normalizedPath
                    : normalizedPath + Path.DirectorySeparatorChar);

            var parent = Path.GetDirectoryName(normalizedPath);
            while (!string.IsNullOrEmpty(parent))
            {
                parentsToRemove.Add(parent);
                parent = Path.GetDirectoryName(parent);
            }
        }

        if (nukedSet.Count == 0) return;

        var keysToRemove = DirectorySizeCache.Keys
            .Where(k =>
                nukedSet.Contains(k) ||
                parentsToRemove.Contains(k) ||
                subtreePrefixes.Any(prefix => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var key in keysToRemove)
        {
            DirectorySizeCache.TryRemove(key, out _);
        }

        SaveMemoryToDisk();
    }
}