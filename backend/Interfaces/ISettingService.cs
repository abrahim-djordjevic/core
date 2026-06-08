using GSInteractiveDeviceAnalyzer.Models.SettingDtos;

namespace GSInteractiveDeviceAnalyzer.Interfaces
{
    public interface ISettingService
    {
        AppSettingDto Current { get; }
        Task<AppSettingDto> LoadAsync();
        Task SaveAsync(AppSettingDto settings);
        event EventHandler<AppSettingDto> OnSettingsChanged;
    }
}
