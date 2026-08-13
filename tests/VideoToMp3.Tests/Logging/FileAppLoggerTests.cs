using VideoToMp3.Infrastructure.Logging;

namespace VideoToMp3.Tests.Logging;

public sealed class FileAppLoggerTests
{
    [Fact]
    public void LogError_WritesSingleLineAndRedactsSecrets()
    {
        using var directory = new TemporaryDirectory();
        var logger = new FileAppLogger(directory.Path);
        var jobId = Guid.NewGuid();

        logger.LogError(
            jobId,
            "Không thể tải\nmedia",
            "https://example.com/v?token=secret&x=1 Authorization: BearerSecret Cookie=session123");

        var log = File.ReadAllText(Path.Combine(directory.Path, "video-to-mp3.log"));
        Assert.Contains($"Job={jobId:N}", log);
        Assert.Contains("[REDACTED]", log);
        Assert.DoesNotContain("secret", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BearerSecret", log);
        Assert.DoesNotContain("session123", log);
        Assert.Single(File.ReadAllLines(Path.Combine(directory.Path, "video-to-mp3.log")));
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
