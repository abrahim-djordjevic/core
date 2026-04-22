using GSInteractiveDeviceAnalyzer;
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
builder.Services.AddScoped<DiskScannerEngine>();
builder.Services.AddScoped<IDiskOperationService, DiskOperationsService>();
var app = builder.Build();

app.UseCors("AllowFlutterApp");
app.UseAuthorization();
app.MapControllers();

app.Run();