using VideoToMp3.Core.Settings;
using VideoToMp3.Infrastructure.Settings;

namespace VideoToMp3.Tests.Settings;

public sealed class JsonSettingsServiceTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsNormalizedSettings()
    {
        using var directory = new TemporaryDirectory();
        var service = new JsonSettingsService(directory.Path);
        service.Save(new AppSettings(@"D:\Music", 192, 4, "Dark", false, false));

        var result = service.Load();

        Assert.Equal(@"D:\Music", result.OutputDirectory);
        Assert.Equal(192, result.Bitrate);
        Assert.Equal(4, result.Concurrency);
        Assert.Equal("Dark", result.Theme);
        Assert.False(result.NotificationsEnabled);
        Assert.False(result.EmbedThumbnail);
    }

    [Fact]
    public void Load_CorruptFileFallsBackToDefaultsWithoutThrowing()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "settings.json"), "{broken-json");

        var result = new JsonSettingsService(directory.Path).Load();

        Assert.Null(result.OutputDirectory);
        Assert.Equal(320, result.Bitrate);
        Assert.Equal(1, result.Concurrency);
        Assert.Equal("System", result.Theme);
        Assert.True(result.NotificationsEnabled);
        Assert.True(result.EmbedThumbnail);
    }

    [Fact]
    public void Load_NormalizesUnsupportedValues()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(directory.Path, "settings.json"),
            """{"bitrate":999,"concurrency":50,"theme":"Unknown"}""");

        var result = new JsonSettingsService(directory.Path).Load();

        Assert.Equal(320, result.Bitrate);
        Assert.Equal(8, result.Concurrency);
        Assert.Equal("System", result.Theme);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "VideoToMp3.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
