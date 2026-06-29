using GSSystemAnalyzer.Interfaces;

namespace GSSystemAnalyzer.Services;

public class LinuxProcessOwnerResolver : IProcessOwnerResolver
{
#if !WINDOWS
    private Dictionary<int, string> _uidToUser = new();
    private DateTime _passwdLastMtime = DateTime.MinValue;

    public LinuxProcessOwnerResolver()
    {
        ReloadPasswdMap();
    }

    public void RefreshCache()
    {
        // Only re-parse /etc/passwd when the file has actually changed
        try
        {
            var mtime = File.GetLastWriteTimeUtc("/etc/passwd");
            if (mtime != _passwdLastMtime)
            {
                ReloadPasswdMap();
            }
        }
        catch
        {
            // Silently ignore — keep the existing cache
        }
    }

    public string Resolve(int processId)
    {
        try
        {
            var statusPath = $"/proc/{processId}/status";
            if (!File.Exists(statusPath)) return "UNKNOWN";

            foreach (var line in File.ReadLines(statusPath))
            {
                if (line.StartsWith("Uid:"))
                {
                    // Format: Uid:\treal\teffective\tsaved\tfs
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var uid))
                    {
                        return _uidToUser.TryGetValue(uid, out var username) ? username : $"UID:{uid}";
                    }

                    break;
                }
            }
        }
        catch
        {
            // Process vanished between check and read — race condition
        }

        return "UNKNOWN";
    }

    private void ReloadPasswdMap()
    {
        try
        {
            var newMap = new Dictionary<int, string>();
            foreach (var line in File.ReadAllLines("/etc/passwd"))
            {
                // Format: username:x:uid:gid:...
                var parts = line.Split(':');
                if (parts.Length >= 3 && int.TryParse(parts[2], out var uid))
                {
                    newMap[uid] = parts[0];
                }
            }

            _uidToUser = newMap;
            _passwdLastMtime = File.GetLastWriteTimeUtc("/etc/passwd");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROCESS OWNER] Failed to parse /etc/passwd: {ex.Message}");
        }
    }
#else
    public void RefreshCache() =>
        throw new PlatformNotSupportedException("LinuxProcessOwnerResolver is only supported on Linux.");

    public string Resolve(int processId) =>
        throw new PlatformNotSupportedException("LinuxProcessOwnerResolver is only supported on Linux.");
#endif
}
