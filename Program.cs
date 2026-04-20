using GSInteractiveDeviceAnalyzer; 

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
var app = builder.Build();

app.UseCors("AllowFlutterApp");
app.UseAuthorization();
app.MapControllers();

app.Run();