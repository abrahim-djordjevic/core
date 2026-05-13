using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using GSInteractiveDeviceAnalyzer.Services;
using GSInteractiveDeviceAnalyzer.Models;

namespace GSInteractiveDeviceAnalyzer.Tests;

public class DuplicateFileDetectorTests
{
    [Fact]
    public async Task FindDuplicatesAsync_WhenGivenDirectoryWithDuplicates_ReturnsCorrectDuplicateGroup()
    {
        // 1. ARRANGE - To build the temporary test lab

        // To generate a random folder name deep inside the computer's temporary files
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // To create two identical files
            File.WriteAllText(Path.Combine(tempDir, "file_A.txt"), "Hello World");
            File.WriteAllText(Path.Combine(tempDir, "file_B.txt"), "Hello World");

            // a completely different file
            File.WriteAllText(Path.Combine(tempDir, "file_C.txt"), "Completely Unique Text");

            var engine = new DuplicateFileDetector();

            // 2. ACT (Fire the engine!!!)
            var results = await engine.FindDuplicatesAsync(tempDir);

            // 3. ASSERT (To prove that the math is correct)

            // Rule 1: We should only find exactly one group of duplicates; the two Hello World files
            Assert.Single(results);

            // Rule 2: Inside that one group, there should be exactly two file paths
            Assert.Equal(2, results[0].FilePaths.Count);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}