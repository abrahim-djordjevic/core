using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GSInteractiveDeviceAnalyzer.Models;
using GSInteractiveDeviceAnalyzer.Services;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Hubs;

namespace GSInteractiveDeviceAnalyzer.BackgroundWorkers;

public class DriveMonitorService : BackgroundService
{
    private readonly IDriveDetectionService _driveService;
    private readonly IHubContext<SystemHub> _hubContext;
    private readonly ILogger<DriveMonitorService> _logger;

    private string _lastHardwareSignature = string.Empty;
    private int _secondsSincelastSpaceCheck = 0;

    private const double AlertThresholdPercent = 90.0;

    public DriveMonitorService(
        IDriveDetectionService driveService,
        IHubContext<SystemHub> hubContext,
        ILogger<DriveMonitorService>logger)
    {
        _driveService = driveService;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Drive Monitor Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var drives = _driveService.GetReadyDrives();

                // Checking for hardware changes (every 5 seconds)
                var currentSignature = string.Join("|", drives.Select(d => $"{d.Name}-{d.Label}-{d.TotalBytes}"));

                if (currentSignature != _lastHardwareSignature)
                {
                    _logger.LogInformation("Hardware change detected. Broadcasting DriveListUpdate.");

                    await _hubContext.Clients.All.SendAsync("DriveListUpdate", new { drives = drives }, stoppingToken);

                    _lastHardwareSignature = currentSignature;
                }

                // Checkinmg for space thresholds (every 60 seconds)
                if (_secondsSincelastSpaceCheck >= 60)
                {
                    foreach (var drive in drives)
                    {
                        if (drive.UsedPercent >= AlertThresholdPercent)
                        {
                            _logger.LogWarning($"Disk Alert: {drive.Name} is critially full ({drive.UsedPercent}%).");

                            await _hubContext.Clients.All.SendAsync("DiskAlert", new
                            {
                                driveName = drive.Name,
                                label = drive.Label,
                                usedPercent = drive.UsedPercent,
                                freeFormatted = FormatSize(drive.FreeBytes)
                            }, stoppingToken);
                        }
                    }
                    _secondsSincelastSpaceCheck = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the Drive Monitor loop.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            _secondsSincelastSpaceCheck += 5;
        }
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