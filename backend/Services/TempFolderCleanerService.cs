using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services;

public class TempFolderCleanerService : ITempFolderCleanerService
{
    private readonly INukeProtocolService _nukeService;
    private readonly ILogger<TempFolderCleanerService> _logger;

    private readonly List<string>? _tempPathsOverride;

    public TempFolderCleanerService(INukeProtocolService nukeService, ILogger<TempFolderCleanerService> logger, IEnumerable<string>? tempPathsOverride = null)
    {
        _nukeService = nukeService;
        _logger = logger;
        _tempPathsOverride = tempPathsOverride?.ToList();
    }

    // Static so unit tests can assert the resolved list directly, and consumers can validate paths.
    public static List<string> ResolveTempPaths()
    {
        var paths = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // %TEMP% — typically C:\Users\<user>\AppData\Local\Temp
            var userTemp = Environment.GetEnvironmentVariable("TEMP");
            if (!string.IsNullOrEmpty(userTemp))
                paths.Add(Path.GetFullPath(userTemp));

            // System-wide temp
            var winTemp = Path.Combine(
                Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\",
                "Windows", "Temp");
            paths.Add(Path.GetFullPath(winTemp));
        }
        else
        {
            // Linux / macOS
            var userCache = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".cache");
            paths.Add(Path.GetFullPath(userCache));

            paths.Add("/tmp");
        }

        // De-dupe (case-insensitive on Windows, case-sensitive elsewhere)
        var comparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        return paths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(comparer)
            .ToList();
    }

    public async Task<TempPreviewResponse> PreviewAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var response = new TempPreviewResponse();
            var tempPaths = _tempPathsOverride ?? ResolveTempPaths();

            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            foreach (var tempDir in tempPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Directory.Exists(tempDir))
                {
                    _logger.LogDebug("Temp path does not exist, omitting from preview: {Path}", tempDir);
                    continue;
                }

                long sizeBytes = 0;
                int fileCount = 0;

                try
                {
                    foreach (var file in new DirectoryInfo(tempDir).EnumerateFiles("*", options))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            sizeBytes += file.Length;
                            fileCount++;
                        }
                        catch
                        {
                            // Locked or permission-denied on individual file — skip silently.
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Entire directory unreadable (permission denied, etc.) — omit it.
                    _logger.LogWarning(ex, "Failed to enumerate temp directory, omitting: {Path}", tempDir);
                    continue;
                }

                response.Locations.Add(new TempLocationPreview
                {
                    Path = tempDir,
                    SizeBytes = sizeBytes,
                    SizeFormatted = FormatSize(sizeBytes),
                    FileCount = fileCount
                });

                response.TotalBytes += sizeBytes;
            }

            response.TotalFormatted = FormatSize(response.TotalBytes);
            return response;
        }, cancellationToken);
    }

    public async Task<TempCleanResult> CleanAsync(List<string> paths, CancellationToken cancellationToken = default)
    {
        var knownPaths = _tempPathsOverride ?? ResolveTempPaths();
        var comparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var knownSet = new HashSet<string>(
            knownPaths.Select(NormalizePath), comparer);

        foreach (var p in paths)
        {
            var normalized = NormalizePath(p);
            if (!knownSet.Contains(normalized))
                throw new UnauthorizedAccessException(
                    $"Path '{p}' is not a recognised temp directory. Only known temp locations may be cleaned.");
        }

        int totalDeleted = 0;
        long totalFreed = 0;
        int totalSkipped = 0;

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        foreach (var p in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tempDir = NormalizePath(p);
            if (!Directory.Exists(tempDir))
                continue;

            // Collect all file paths inside this temp directory.
            var filePaths = new List<string>();
            try
            {
                foreach (var file in new DirectoryInfo(tempDir).EnumerateFiles("*", options))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    filePaths.Add(file.FullName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enumerate files in temp dir: {Path}", tempDir);
            }

            if (filePaths.Count == 0)
                continue;

            // Delegate to the Nuke service: preview (required for plan token) → obliterate.
            var preview = await _nukeService.PreviewNukeAsync(filePaths, cancellationToken);
            var nukeResult = await _nukeService.ObliterateNodeAsync(filePaths, preview.PlanToken, useRecycleBin: false, cancellationToken);

            totalDeleted += nukeResult.DeletedFiles;
            totalFreed += nukeResult.FreedBytes;
            totalSkipped += nukeResult.SkippedFiles;

            // Clean up empty subdirectories left behind (bottom-up).
            // The temp directory itself is NEVER deleted.
            CleanEmptySubdirectories(tempDir);
        }

        return new TempCleanResult
        {
            DeletedFiles = totalDeleted,
            FreedBytes = totalFreed,
            FreedFormatted = FormatSize(totalFreed),
            SkippedFiles = totalSkipped
        };
    }

    private static string NormalizePath(string path)
    {
        var full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private void CleanEmptySubdirectories(string tempDir)
    {
        var normalizedRoot = NormalizePath(tempDir);

        try
        {
            // Use a non-recursive first pass to collect only real (non-reparse) subdirectories,
            // then recurse manually. This avoids following symlinks/junctions.
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false,
                AttributesToSkip = FileAttributes.ReparsePoint // skip symlinks & junctions
            };

            var subdirs = new DirectoryInfo(tempDir)
                .EnumerateDirectories("*", options)
                .Select(d => d.FullName)
                .OrderByDescending(d => d.Length) // deepest first
                .ToList();

            foreach (var dir in subdirs)
            {
                // Double-guard: never delete the root temp directory itself.
                if (string.Equals(NormalizePath(dir), normalizedRoot,
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal))
                    continue;

                try
                {
                    if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                        Directory.Delete(dir);
                }
                catch
                {
                    // Locked or permission-denied — skip silently.
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not clean empty subdirectories in {TempDir}", tempDir);
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return string.Format("{0:n1} {1}", number, suffixes[counter]);
    }
}
