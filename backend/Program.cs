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
builder.Services.AddScoped<IDiskOperationService, DiskOperationsService>();
builder.Services.AddSignalR();
var app = builder.Build();

app.UseCors("AllowFlutterApp");
app.UseAuthorization();
app.MapControllers();
app.MapHub<StorageHub>("/storageHub");

// Testing duplicate file detector
// app.MapGet("/test-duplicates", async (DuplicateFileDetector myEngine) => 
// {
//     string testFolder = @"C:\Users\AHMED IKEOLUWA\Pictures";

//     var cts = new CancellationTokenSource();
//     var results = await myEngine.FindDuplicatesAsync(testFolder, cts.Token);

//     return Results.Ok(new
//     {
//         Message = "Scan Complete!",
//         TotalDuplicatesFound = results.Count, 
//         Data = results
//     });
// });

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var engine = app.Services.GetRequiredService<DiskScannerEngine>();

lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("SERVER SHUTTING DOWN: Backing up memory to disk...");
    engine.SaveMemoryToDisk();
});

app.Run();