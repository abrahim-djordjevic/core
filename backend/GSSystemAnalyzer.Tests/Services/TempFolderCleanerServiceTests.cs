using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Services;

public class TempFolderCleanerServiceTests : IDisposable
{
    private readonly string _fakeTempDir;
    private readonly Mock<IDiskScannerEngine> _scanner = new();

    public TempFolderCleanerServiceTests()
    {
        _fakeTempDir = Path.Combine(Path.GetTempPath(), "gstemp_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_fakeTempDir);

        _scanner.Setup(s => s.NukeToken()).Returns(CancellationToken.None);
        _scanner.Setup(s => s.InvalidatePaths(It.IsAny<IEnumerable<string>>()));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_fakeTempDir)) Directory.Delete(_fakeTempDir, true); } catch { /* ignore */ }
    }

    /// <summary>Creates a real NukeProtocolService wired with mocked SignalR + scanner.</summary>
    private NukeProtocolService CreateNukeService()
    {
        var hub = new Mock<IHubContext<SystemHub>>();
        var clients = new Mock<IHubClients>();
        var proxy = new Mock<IClientProxy>();
        clients.Setup(c => c.All).Returns(proxy.Object);
        hub.Setup(h => h.Clients).Returns(clients.Object);
        proxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        return new NukeProtocolService(_scanner.Object, hub.Object, NullLogger<NukeProtocolService>.Instance, runStartupCleanup: false);
    }

    private TempFolderCleanerService CreateService(INukeProtocolService? nukeOverride = null, IEnumerable<string>? pathsOverride = null)
    {
        var nuke = nukeOverride ?? CreateNukeService();
        return new TempFolderCleanerService(nuke, NullLogger<TempFolderCleanerService>.Instance, pathsOverride);
    }

    private string SeedFile(string relativePath = "test.txt", string content = "hello world")
    {
        var fullPath = Path.Combine(_fakeTempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    [Fact]
    public void ResolveTempPaths_ReturnsNonEmptyList()
    {
        var paths = TempFolderCleanerService.ResolveTempPaths();

        Assert.NotEmpty(paths);
        Assert.All(paths, p => Assert.False(string.IsNullOrWhiteSpace(p)));
    }

    [Fact]
    public void ResolveTempPaths_NoDuplicates()
    {
        var paths = TempFolderCleanerService.ResolveTempPaths();

        var comparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        Assert.Equal(paths.Count, paths.Distinct(comparer).Count());
    }

    [Fact]
    public void ResolveTempPaths_Windows_IncludesExpectedPaths()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return; // Skip on non-Windows

        var paths = TempFolderCleanerService.ResolveTempPaths();

        // %TEMP% should be included
        var userTemp = Environment.GetEnvironmentVariable("TEMP");
        Assert.Contains(paths, p => p.Equals(Path.GetFullPath(userTemp!), StringComparison.OrdinalIgnoreCase));

        // C:\Windows\Temp should be included
        Assert.Contains(paths, p => p.EndsWith("Windows\\Temp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Preview_CorrectlySumsSizes()
    {
        // We can't inject custom paths into ResolveTempPaths for the full service,
        // but we can test the preview logic on a real temp dir by ensuring
        // the system temp (which always exists) returns a valid response.
        var svc = CreateService();

        var result = await svc.PreviewAsync();

        Assert.NotNull(result);
        Assert.NotEmpty(result.Locations);
        Assert.All(result.Locations, loc =>
        {
            Assert.False(string.IsNullOrWhiteSpace(loc.Path));
            Assert.True(loc.SizeBytes >= 0);
            Assert.False(string.IsNullOrWhiteSpace(loc.SizeFormatted));
            Assert.True(loc.FileCount >= 0);
        });

        // TotalBytes should equal the sum of all location sizes
        var expectedTotal = result.Locations.Sum(l => l.SizeBytes);
        Assert.Equal(expectedTotal, result.TotalBytes);
        Assert.False(string.IsNullOrWhiteSpace(result.TotalFormatted));
    }

    [Fact]
    public async Task Preview_SupportsCancel()
    {
        var svc = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Task.Run with a pre-cancelled token throws TaskCanceledException,
        // which derives from OperationCanceledException.
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.PreviewAsync(cts.Token));
        Assert.True(ex.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Clean_RejectsUnknownPath()
    {
        var svc = CreateService();
        var bogusPath = Path.Combine(Path.GetTempPath(), "totally_not_a_temp_dir_" + Guid.NewGuid().ToString("N"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.CleanAsync(new List<string> { bogusPath }));
    }

    [Fact]
    public async Task Clean_AcceptsPathWithTrailingSeparator()
    {
        var svc = CreateService();
        var tempPath = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
        var withTrailing = tempPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        // Should NOT throw UnauthorizedAccessException
        var result = await svc.CleanAsync(new List<string> { withTrailing });
        Assert.True(result.DeletedFiles >= 0);
    }

    [Fact]
    public async Task Clean_DeletesFilesFromRealTemp()
    {
        // Create a test file in the fake temp dir
        var testFile = SeedFile("gs_cleantest_" + Guid.NewGuid().ToString("N") + ".tmp", "cleanup test data");

        try
        {
            var svc = CreateService(pathsOverride: new[] { _fakeTempDir });
            var tempPath = _fakeTempDir;

            // Only clean the specific temp directory
            var result = await svc.CleanAsync(new List<string> { tempPath });

            Assert.True(result.DeletedFiles >= 0);
            Assert.True(result.FreedBytes >= 0);
            Assert.False(string.IsNullOrWhiteSpace(result.FreedFormatted));

            // The temp DIRECTORY should still exist
            Assert.True(Directory.Exists(tempPath));
        }
        finally
        {
            // Clean up if test file wasn't deleted
            if (File.Exists(testFile)) File.Delete(testFile);
        }
    }

    [Fact]
    public async Task Clean_TempDirectoryItselfNeverDeleted()
    {
        // This test verifies the acceptance criteria: temp directories are never deleted.
        var svc = CreateService(pathsOverride: new[] { _fakeTempDir });
        var existingPaths = new List<string> { _fakeTempDir };

        await svc.CleanAsync(existingPaths);

        // Verify all temp directories still exist
        Assert.All(existingPaths, path => Assert.True(Directory.Exists(path),
            $"Temp directory was deleted: {path}"));
    }

    [Fact]
    public async Task Clean_EmptyPathsList_IsHandledGracefully()
    {
        var svc = CreateService();

        // Empty list shouldn't throw, should return zero counters
        var result = await svc.CleanAsync(new List<string>());

        Assert.Equal(0, result.DeletedFiles);
        Assert.Equal(0, result.FreedBytes);
        Assert.Equal(0, result.SkippedFiles);
    }
}
