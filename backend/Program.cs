using GSInteractiveDeviceAnalyzer;
using GSInteractiveDeviceAnalyzer.Engine;
using GSInteractiveDeviceAnalyzer.Hubs;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Services;


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
builder.Services.AddScoped<IDiskOperationService, DiskOperationsService>();
builder.Services.AddScoped<IDuplicateFileDetector, DuplicateFileDetector>();
builder.Services.AddSignalR();
var app = builder.Build();

app.UseCors("AllowFlutterApp");
app.UseAuthorization();
app.MapControllers();
app.MapHub<StorageHub>("/storageHub");

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