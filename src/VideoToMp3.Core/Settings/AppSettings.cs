namespace VideoToMp3.Core.Settings;

public sealed record AppSettings(
    string? OutputDirectory = null,
    int Bitrate = 320,
    int Concurrency = 1,
    string Theme = "System",
    bool NotificationsEnabled = true);
