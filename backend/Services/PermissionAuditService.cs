using System.Runtime.InteropServices;
using System.Security.Principal;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;

#if WINDOWS
using System.Security.AccessControl;
#endif

namespace GSSystemAnalyzer.Services;

public class PermissionAuditService : IPermissionAuditService
{
    private readonly ISettingService _settings;
    private readonly ILogger<PermissionAuditService> _logger;

    // Extensions that should not live in user data directories
    private static readonly HashSet<string> ExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".ps1", ".sh", ".msi", ".scr", ".com"
    };

    // Windows: paths where executables are expected
    private static readonly string[] WindowsSystemPaths =
    {
        @"C:\Windows",
        @"C:\Program Files",
        @"C:\Program Files (x86)"
    };

    // Linux: paths where executables are expected
    private static readonly string[] LinuxSystemPaths =
    {
        "/usr", "/bin", "/sbin", "/opt", "/snap"
    };

    // Windows: identities that indicate world-accessible permissions
    private static readonly HashSet<string> WorldIdentities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Everyone",
        "Authenticated Users",
        @"BUILTIN\Users",
        "Users"
    };

    public PermissionAuditService(ISettingService settings, ILogger<PermissionAuditService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<PermissionAuditResult> AuditAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var config = _settings.Current.Scan;

        return await Task.Run(() =>
        {
            var issues = new List<PermissionIssue>();
            int totalScanned = 0;

            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false,
                MaxRecursionDepth = config.Depth,
                AttributesToSkip = 0
            };

            if (config.SkipHiddenFiles) options.AttributesToSkip |= FileAttributes.Hidden;
            if (config.SkipSystemFiles) options.AttributesToSkip |= FileAttributes.System;

            // Normalize excluded paths for fast prefix matching
            var excludedPaths = config.ExcludedPaths
                .Select(p => Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .ToList();

            var rootDir = new DirectoryInfo(rootPath);

            // Audit directories
            foreach (var dir in rootDir.EnumerateDirectories("*", options))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsExcluded(dir.FullName, excludedPaths)) continue;

                totalScanned++;
                AuditEntry(dir.FullName, isDirectory: true, issues);
            }

            // Audit files
            foreach (var file in rootDir.EnumerateFiles("*", options))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsExcluded(file.FullName, excludedPaths)) continue;

                totalScanned++;
                AuditEntry(file.FullName, isDirectory: false, issues);
            }

            return new PermissionAuditResult
            {
                Root = rootPath,
                AuditedAt = DateTime.UtcNow,
                TotalScanned = totalScanned,
                Issues = issues
            };
        }, cancellationToken);
    }

    private void AuditEntry(string path, bool isDirectory, List<PermissionIssue> issues)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AuditWindows(path, isDirectory, issues);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            AuditLinux(path, isDirectory, issues);
        }

        // Executable-in-data-dir is platform-independent (extension + path check only)
        if (!isDirectory)
        {
            var systemPaths = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? WindowsSystemPaths
                : LinuxSystemPaths;
            CheckExecutableInDataDir(path, systemPaths, issues);
        }
    }

    private void AuditWindows(string path, bool isDirectory, List<PermissionIssue> issues)
    {
#if WINDOWS
        try
        {
            // Check for world-writable ACEs
            FileSystemSecurity security;
            if (isDirectory)
                security = new DirectoryInfo(path).GetAccessControl();
            else
                security = new FileInfo(path).GetAccessControl();

            var rules = security.GetAccessRules(true, true, typeof(NTAccount));

            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.AccessControlType != AccessControlType.Allow) continue;

                var identity = rule.IdentityReference.Value;

                // Check if it's a world identity and has write/full control
                var isWorldIdentity = WorldIdentities.Any(w =>
                    identity.EndsWith(w, StringComparison.OrdinalIgnoreCase));

                if (!isWorldIdentity) continue;

                var hasWrite = rule.FileSystemRights.HasFlag(FileSystemRights.Write) ||
                               rule.FileSystemRights.HasFlag(FileSystemRights.FullControl) ||
                               rule.FileSystemRights.HasFlag(FileSystemRights.Modify);

                if (hasWrite)
                {
                    issues.Add(new PermissionIssue
                    {
                        Path = path,
                        Severity = "medium",
                        Type = "world_writable",
                        Description = isDirectory
                            ? $"Directory is writable by all users ({identity}: {rule.FileSystemRights})"
                            : $"File is writable by all users ({identity}: {rule.FileSystemRights})"
                    });
                    break; // One world-writable flag is enough per entry
                }
            }

            // Check for orphaned / no owner
            try
            {
                var owner = security.GetOwner(typeof(NTAccount));
                if (owner == null)
                {
                    issues.Add(new PermissionIssue
                    {
                        Path = path,
                        Severity = "low",
                        Type = "no_owner",
                        Description = "File system entry has no identifiable owner"
                    });
                }
            }
            catch
            {
                issues.Add(new PermissionIssue
                {
                    Path = path,
                    Severity = "low",
                    Type = "no_owner",
                    Description = "Could not resolve owner — possible orphaned SID"
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Inaccessible — silently skip, not an error
        }
        catch (Exception ex)
        {
            _logger.LogDebug("ACL read failed for {Path}: {Error}", path, ex.Message);
        }
#endif
    }

    private void AuditLinux(string path, bool isDirectory, List<PermissionIssue> issues)
    {
        try
        {
            var mode = File.GetUnixFileMode(path);

            // World-writable: OtherWrite bit set
            if (mode.HasFlag(UnixFileMode.OtherWrite))
            {
                issues.Add(new PermissionIssue
                {
                    Path = path,
                    Severity = "medium",
                    Type = "world_writable",
                    Description = isDirectory
                        ? $"Directory has world-write permission (mode: {mode})"
                        : $"File has world-write permission (mode: {mode})"
                });
            }

            // Executable in data dir: execute bit set outside standard paths
            if (!isDirectory && mode.HasFlag(UnixFileMode.UserExecute))
            {
                CheckExecutableInDataDir(path, LinuxSystemPaths, issues);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Silently skip inaccessible entries
        }
        catch (Exception ex)
        {
            _logger.LogDebug("stat() failed for {Path}: {Error}", path, ex.Message);
        }
    }


    private static void CheckExecutableInDataDir(string path, string[] systemPaths, List<PermissionIssue> issues)
    {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext)) return;
        if (!ExecutableExtensions.Contains(ext)) return;

        var fullPath = Path.GetFullPath(path);
        var isInSystemPath = systemPaths.Any(sp =>
            fullPath.StartsWith(sp, StringComparison.OrdinalIgnoreCase));

        if (!isInSystemPath)
        {
            // Determine a human-friendly location name
            var parentDir = Path.GetDirectoryName(fullPath) ?? fullPath;
            var folderName = Path.GetFileName(parentDir);

            issues.Add(new PermissionIssue
            {
                Path = path,
                Severity = "high",
                Type = "executable_in_data_dir",
                Description = $"Executable ({ext}) found in {folderName} directory"
            });
        }
    }

    private static bool IsExcluded(string fullPath, List<string> excludedPaths)
    {
        return excludedPaths.Any(ep =>
            fullPath.StartsWith(ep, StringComparison.OrdinalIgnoreCase));
    }
}
