namespace VideoToMp3.Core.Settings;

public sealed record AppSettings(
    string? OutputDirectory = null,
    int Bitrate = 320,
    int Concurrency = 2,
    string Theme = "System",
    bool NotificationsEnabled = true,
    bool EmbedThumbnail = true,
    bool UseChromeCookies = false,
    string CookieBrowser = "Firefox");
