using System.Collections.Concurrent;
using System.Management;
using System.Runtime.InteropServices;
using GSInteractiveDeviceAnalyzer.Interfaces;

namespace GSInteractiveDeviceAnalyzer.Services;

public class WindowsProcessOwnerResolver : IProcessOwnerResolver
{
#if WINDOWS
    private ConcurrentDictionary<int, string> _ownerCache = new();

    public void RefreshCache()
    {
        var newCache = new ConcurrentDictionary<int, string>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, Handle FROM Win32_Process");

            using var results = searcher.Get();

            foreach (ManagementObject process in results)
            {
                try
                {
                    var pid = Convert.ToInt32(process["ProcessId"]);
                    var outParams = process.InvokeMethod("GetOwner", null, null);

                    if (outParams != null && Convert.ToInt32(outParams["ReturnValue"]) == 0)
                    {
                        var user = outParams["User"]?.ToString() ?? "SYSTEM";
                        newCache[pid] = user;
                    }
                    else
                    {
                        newCache[pid] = "SYSTEM";
                    }
                }
                catch
                {
                    // AccessDeniedException on system-protected processes — fall back silently
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROCESS OWNER] WMI batch query failed: {ex.Message}");
        }

        _ownerCache = newCache;
    }

    public string Resolve(int processId)
    {
        return _ownerCache.TryGetValue(processId, out var owner) ? owner : "SYSTEM";
    }
#else
    public void RefreshCache() =>
        throw new PlatformNotSupportedException("WindowsProcessOwnerResolver is only supported on Windows.");

    public string Resolve(int processId) =>
        throw new PlatformNotSupportedException("WindowsProcessOwnerResolver is only supported on Windows.");
#endif
}
