using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using GSInteractiveDeviceAnalyzer.Models;
using GSInteractiveDeviceAnalyzer.Interfaces;
using System.Runtime.CompilerServices;

namespace GSInteractiveDeviceAnalyzer.Services;
public class LargeFileHunterService : ILargeFileHunterService
{
    public async Task<List<LargeFile>> GetTopLargeFilesAsync(string rootPath, int topN, CancellationToken cancellationToken = default)
    {
        // To offload the heavy hardrive I/O to a background thread
        return await Task.Run(() =>
        {
            // The Min-Heap. 'long' is the priority (the file size)
            var topFiles = new PriorityQueue<LargeFile, long>();

            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true, // This silently skips locked system folders
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false
            };

            foreach (var filePath in Directory.EnumerateFiles(rootPath, "*", options))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var fileInfo = new FileInfo (filePath);
                    long size = fileInfo.Length;

                    if (topFiles.Count < topN)
                    {
                        // We haven't reached the limit yet, so add it
                        topFiles.Enqueue(new LargeFile { Path = filePath, SizeBytes = size }, size);
                    } else if (size > topFiles.Peek().SizeBytes)
                    {
                        // Means the queue is full. Kick out the smallest file, and add the new bigger one
                        topFiles.Dequeue();
                        topFiles.Enqueue(new LargeFile { Path = filePath, SizeBytes = size }, size);
                    }
                }
                catch
                {
                    // just in case
                    continue;
                }
            }

            // To extract the files from the queue into a list
            var result = new List<LargeFile>();
            while (topFiles.Count > 0)
            {
                result.Add(topFiles.Dequeue());
            }

            // A min-heap pops the smallest files files first, so we reverse it so the largest is at index 0
            result.Reverse();

            // To format the raw bytes into human-readable strings
            foreach (var file in result)
            {
                file.SizeFormatted = FormatSize(file.SizeBytes);
            }

            return result;
        });
    }

    private string FormatSize(long bytes)
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