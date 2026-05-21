using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using GSInteractiveDeviceAnalyzer.Models;
using GSInteractiveDeviceAnalyzer.Services;

namespace GSInteractiveDeviceAnalyzer.Tests;

public class NukeProtocolServiceTests : IDisposable
{
    private readonly string _sandboxRoot;
    private readonly NukeProtocolService _service;

    public NukeProtocolServiceTests()
    {
        _sandboxRoot = Path.Combine(Path.GetTempPath(), $"NukeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_sandboxRoot);
        _service = new NukeProtocolService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_sandboxRoot))
        {
            Directory.Delete(_sandboxRoot, true);
        }
    }

    [Fact]
    public async Task Preview_IsStrictlyReadOnly_DoesNotDeleteFiles()
    {
        // Arrange
        var testFile = Path.Combine(_sandboxRoot, "keep_me.txt");
        await File.WriteAllTextAsync(testFile, "This data must survive.");
        var paths = new List <string> { _sandboxRoot };

        // Act
        var result = await _service.PreviewNukeAsync(paths, CancellationToken.None);

        // Assert
        Assert.True(File.Exists(testFile), "The preview endpoint accidentally deleted a file!");
        Assert.Equal(1, result.TotalFiles);
    }

    [Fact]
    public async Task Preview_CorrectlySumsSizesrecursively_ForNestedDirectories()
    {
        // Arrange
        var subFolder = Path.Combine(_sandboxRoot, "SubFolder");
        var subsubFolder = Path.Combine(subFolder, "SubSubFolder");
        Directory.CreateDirectory(subsubFolder);

        var file1 = Path.Combine(subFolder, "file1.dat");
        var file2 = Path.Combine(subsubFolder, "file2.dat");

        await File.WriteAllBytesAsync(file1, new byte[10]);
        await File.WriteAllBytesAsync(file2, new byte[20]);

        var paths = new List<string> { _sandboxRoot };

        // Act
        var result = await _service.PreviewNukeAsync(paths, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(30, result.TotalBytes);
    }

    [Fact]
    public async Task Preview_InaccesibleFiles_AreExcludedWithoutCrashing()
    {
        // Arrange
        var accessibleFile = Path.Combine(_sandboxRoot, "open.txt");
        await File.WriteAllBytesAsync(accessibleFile, new byte[50]);

        var fakeRoute = Path.Combine(_sandboxRoot, "NonExistentSecureFolder");

        var paths = new List<string> { _sandboxRoot, fakeRoute };

        // Act
        var result = await _service.PreviewNukeAsync(paths, CancellationToken.None);

        // Assert: this should skip the fake file and count the open one only
        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(50, result.TotalBytes);
        
    }

    [Theory]
    [InlineData(0, "0.0 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(5242880, "5.0 MB")]
    public async Task Preview_TotalFormatted_MatchesTotalBytesInHumanReadableForm(long byteSize, string expectedFormat)
    {
        // Arrange
        if (byteSize > 0)
        {
            var file = Path.Combine(_sandboxRoot, "format_test.dat");
            await File.WriteAllBytesAsync(file, new byte[byteSize]);
        }
        var paths = new List<string> { _sandboxRoot };

        // Act
        var result = await _service.PreviewNukeAsync(paths, CancellationToken.None);

        // Assert
        Assert.Equal(expectedFormat, result.TotalFormatted);
    }

    [Fact]
    public async Task Preview_EmptyPathsArray_ReturnsZeroTotals()
    {
        // Arrange 
        var paths = new List<string>();

        // Act
        var result = await _service.PreviewNukeAsync(paths, CancellationToken.None);

        // Assert (empty paths array returns zero totals)
        Assert.Equal(0, result.TotalFiles);
        Assert.Equal(0, result.TotalBytes);
        Assert.Equal("0.0 B", result.TotalFormatted);
        Assert.Empty(result.Breakdown);
    }
}