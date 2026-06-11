using GSInteractiveDeviceAnalyzer;
using GSInteractiveDeviceAnalyzer.BackgroundWorkers;
using GSInteractiveDeviceAnalyzer.Engine;
using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Services;
using LibreHardwareMonitor.Hardware;
using System.Runtime.InteropServices;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DiskScannerEngine>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterApp", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddSingleton<DiskScannerEngine>();
builder.Services.AddSingleton<RamMonitoringEngine>();
builder.Services.AddSingleton<DuplicateFileDetector>();
builder.Services.AddSingleton<LargeFileHunterService>();
builder.Services.AddSingleton<ILargeFileHunterService, LargeFileHunterService>();
builder.Services.AddSingleton<INukeProtocolService, NukeProtocolService>();
builder.Services.AddSingleton<IDriveDetectionService, DriveDetectionService>();
builder.Services.AddSingleton<ISettingService, SettingsServices>();
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddSingleton<ICpuMetricsProvider, WindowsCpuProvider>();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Services.AddSingleton<ICpuMetricsProvider, LinuxCpuProvider>();
}
else
{
    throw new PlatformNotSupportedException("OS not supported for CPU telemetry");
}
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddSingleton<IThermalProvider, LibreThermalProvider>();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Services.AddSingleton<IThermalProvider, LinuxThermalProvider>();
}
else
{
    throw new PlatformNotSupportedException("OS not supported for CPU telemetry");
}
builder.Services.AddHostedService<CpuSamplerEngine>();
builder.Services.AddSingleton<ILargeFileHunterService, LargeFileHunterService>();
builder.Services.AddSingleton<INukeProtocolService, NukeProtocolService>();
builder.Services.AddHostedService<ThermalMonitoringEngine>();
builder.Services.AddHostedService<DriveMonitorService>();
builder.Services.AddScoped<IDiskOperationService, DiskOperationsService>();
builder.Services.AddScoped<IDuplicateFileDetector, DuplicateFileDetector>();
builder.Services.AddSignalR();
var app = builder.Build();

app.UseCors("AllowFlutterApp");
app.UseAuthorization();
app.MapControllers();
app.MapHub<SystemHub>("/systemHub");

// Health check endpoints
app.MapGet("/", () => new { status = "Server is running", timestamp = DateTime.UtcNow });
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));



var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var engine = app.Services.GetRequiredService<DiskScannerEngine>();

lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("SERVER SHUTTING DOWN: Backing up memory to disk...");
    engine.SaveMemoryToDisk();
});

app.Run();