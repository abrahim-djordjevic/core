using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GSInteractiveDeviceAnalyzer // (Note: Change this namespace to match the other files in his repo!)
{
    public class DuplicateFileDetector
    {
        /// <summary>
        /// Scans a root directory and returns a dictionary of duplicate files grouped by their SHA256 hash.
        /// </summary>
        public async Task<Dictionary<string, List<string>>> FindDuplicatesAsync(string rootPath, CancellationToken cancellationToken = default)
        {
            // 1. Safely gather all files, ignoring protected system folders (Fixes the "Ghost Reading" crash)
            var allFiles = SafeEnumerateFiles(rootPath);

            // --- PASS 1: The O(n) Size Filter ---
            // Group files by size. If a size group only has 1 file, throw it away.
            var filesToHash = allFiles
                .GroupBy(f => f.Length)
                .Where(group => group.Count() > 1)
                .SelectMany(group => group)
                .ToList();

            // --- PASS 2: The Parallel Hash ---
            // Thread-safe dictionary to handle multiple CPU cores writing at the same time
            var hashedFiles = new ConcurrentDictionary<string, ConcurrentBag<string>>();

            // Process the remaining files across all available CPU cores
            await Parallel.ForEachAsync(filesToHash, async (file, cancellationToken) =>
            {
                try
                {
                    using var sha256 = SHA256.Create();
                    using var stream = File.OpenRead(file.FullName);
                    
                    byte[] hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
                    string hashString = BitConverter.ToString(hashBytes).Replace("-", "");

                    hashedFiles.AddOrUpdate(
                        hashString,
                        new ConcurrentBag<string> { file.FullName },
                        (key, existingBag) => 
                        {
                            existingBag.Add(file.FullName);
                            return existingBag;
                        });
                }
                catch (IOException) 
                {
                    // Skip files that are currently locked or open in another program
                }
                catch (UnauthorizedAccessException) 
                {
                    // Skip files we don't have permission to read
                }
            });

            // 3. Final Cleanup: Convert back to standard collections and filter out unique files
            return hashedFiles
                .Where(kvp => kvp.Value.Count > 1)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
        }

        /// <summary>
        /// Helper method to traverse directories without crashing on UnauthorizedAccessException.
        /// </summary>
        private IEnumerable<FileInfo> SafeEnumerateFiles(string rootPath)
        {
            var rootDir = new DirectoryInfo(rootPath);
            var files = Enumerable.Empty<FileInfo>();

            try { files = rootDir.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly); }
            catch (UnauthorizedAccessException) { /* Ignore protected folders */ }
            catch (DirectoryNotFoundException) { /* Ignore deleted folders */ }

            foreach (var file in files)
            {
                yield return file; // yield return is highly memory efficient!
            }

            var directories = Enumerable.Empty<DirectoryInfo>();
            try { directories = rootDir.EnumerateDirectories("*.*", SearchOption.TopDirectoryOnly); }
            catch (UnauthorizedAccessException) { }

            foreach (var dir in directories)
            {
                // Recursive call to dive into subdirectories
                foreach (var file in SafeEnumerateFiles(dir.FullName))
                {
                    yield return file;
                }
            }
        }
    }
}