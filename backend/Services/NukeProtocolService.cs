using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GSInteractiveDeviceAnalyzer.Models;
using GSInteractiveDeviceAnalyzer.Interfaces;

namespace GSInteractiveDeviceAnalyzer.Services;

public class NukeProtocolService : INukeProtocolService
{
    public async Task<NukePreviewResponse> PreviewNukeAsync(List<string> paths, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var response = new NukePreviewResponse();

            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false
            };

            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Skip if the user sent a garbage path
                if (!Directory.Exists(path))
                {
                    continue;
                }

                long pathBytes = 0;
                int pathFileCount = 0;

                // The Zero Allocation Counter
                // We parse the raw OS struct, update the tally, and discard the object instantly
                var counter = new System.IO.Enumeration.FileSystemEnumerable<byte>(
                    path,
                    (ref System.IO.Enumeration.FileSystemEntry entry) =>
                    {
                        if (!entry.IsDirectory)
                        {
                            pathBytes += entry.Length;
                            pathFileCount++;
                        }
                        return 0;
                    },
                    options
                );

                foreach (var _ in counter)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                response.Breakdown.Add(new NukePathBreakdown
                {
                    Path = path,
                    SizeBytes = pathBytes,
                    FileCount = pathFileCount
                });

                response.TotalBytes += pathBytes;
                response.TotalFiles += pathFileCount;
            }

            response.TotalFormatted = FormatSize(response.TotalBytes);

            return response;

        }, cancellationToken);
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