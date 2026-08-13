using VideoToMp3.Core.Settings;

namespace VideoToMp3.Core.Services;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
