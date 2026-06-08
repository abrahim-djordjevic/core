using System.Text.Json;
using GSInteractiveDeviceAnalyzer.Interfaces;
using GSInteractiveDeviceAnalyzer.Models.SettingDtos;

namespace GSInteractiveDeviceAnalyzer.Services
{
    public class SettingsServices : ISettingService
    {
        private readonly string _settingsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _fileLoack = new();

        public AppSettingDto Current { get; private set; }
        public event EventHandler<AppSettingDto>? OnSettingsChanged;


        public SettingsServices(string? testFilePath = null)
        {
            if (testFilePath != null)
            {
                _settingsFilePath = testFilePath;
            }
            else
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appData, "GSAnalyzer");
                Directory.CreateDirectory(appFolder);

                _settingsFilePath = Path.Combine(appFolder, "appsettings.user.json");
            }
            
            _jsonOptions = new JsonSerializerOptions
                { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            Current = LoadAsync().GetAwaiter().GetResult();
        }

        public async Task<AppSettingDto> LoadAsync()
        {
            if (!File.Exists(_settingsFilePath))
            {
                var defaults = AppSettingDto.GetFactoryDefaults();
                await SaveAsync(defaults);
                return defaults;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettingDto>(json, _jsonOptions);
                return settings ?? AppSettingDto.GetFactoryDefaults();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SETTINGS] Corrupt config file Detected. Restoring defaults. Error: {ex.Message}");
                var defaults = AppSettingDto.GetFactoryDefaults();
                await SaveAsync(defaults);
                return defaults;
            }
        }

        public async Task SaveAsync(AppSettingDto settings)
        {
            Current = settings;
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            var tempPath = _settingsFilePath + ".tmp";

            lock (_fileLoack)
            {
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _settingsFilePath, overwrite: true);
            }

            OnSettingsChanged?.Invoke(this, Current);

            await Task.CompletedTask;
        }
    }
}
