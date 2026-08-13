using System.Text.Json;
using VideoToMp3.Core.Services;
using VideoToMp3.Core.Settings;

namespace VideoToMp3.Infrastructure.Settings;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    private readonly object _syncRoot = new();
    private readonly string _settingsFilePath;

    public JsonSettingsService(string? settingsDirectory = null)
    {
        var directory = settingsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoToMp3");
        _settingsFilePath = Path.Combine(directory, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            lock (_syncRoot)
            {
                if (!File.Exists(_settingsFilePath))
                {
                    return new AppSettings();
                }

                var settings = JsonSerializer.Deserialize<AppSettings>(
                    File.ReadAllText(_settingsFilePath),
                    JsonOptions);
                return Normalize(settings ?? new AppSettings());
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        try
        {
            lock (_syncRoot)
            {
                var directory = Path.GetDirectoryName(_settingsFilePath)!;
                Directory.CreateDirectory(directory);
                var temporaryPath = $"{_settingsFilePath}.{Guid.NewGuid():N}.tmp";
                try
                {
                    File.WriteAllText(
                        temporaryPath,
                        JsonSerializer.Serialize(Normalize(settings), JsonOptions));
                    File.Move(temporaryPath, _settingsFilePath, overwrite: true);
                }
                finally
                {
                    File.Delete(temporaryPath);
                }
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Settings persistence is best effort and must not crash the app.
        }
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        var bitrate = settings.Bitrate is 128 or 192 or 256 or 320 ? settings.Bitrate : 320;
        var theme = settings.Theme is "Light" or "Dark" or "System" ? settings.Theme : "System";
        var cookieBrowser = settings.CookieBrowser is "Firefox" or "Chrome" or "Edge"
            ? settings.CookieBrowser
            : "Firefox";
        return settings with
        {
            OutputDirectory = string.IsNullOrWhiteSpace(settings.OutputDirectory)
                ? null
                : settings.OutputDirectory,
            Bitrate = bitrate,
            Concurrency = Math.Clamp(settings.Concurrency, 1, 4),
            Theme = theme,
            CookieBrowser = cookieBrowser
        };
    }
}
