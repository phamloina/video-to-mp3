using VideoToMp3.Core.Dependencies;
using VideoToMp3.Infrastructure.Dependencies;
using VideoToMp3.Infrastructure.Processes;

namespace VideoToMp3.Tests.Dependencies;

public sealed class MediaToolResolverTests
{
    [Theory]
    [InlineData(MediaTool.Ffmpeg, "ffmpeg", "ffmpeg.exe")]
    [InlineData(MediaTool.Ffprobe, "ffmpeg", "ffprobe.exe")]
    [InlineData(MediaTool.YtDlp, "yt-dlp", "yt-dlp.exe")]
    public void Resolve_PrefersManagedToolDirectory(
        MediaTool tool,
        string toolDirectory,
        string executableName)
    {
        using var fixture = new ToolDirectoryFixture();
        var executablePath = fixture.CreateTool(toolDirectory, executableName);
        var resolver = new MediaToolResolver(fixture.ApplicationDirectory);

        var result = resolver.Resolve(tool);

        Assert.True(result.IsAvailable);
        Assert.Equal(Path.GetFullPath(executablePath), result.ExecutablePath);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Resolve_ReturnsStructuredMissingDependencyResult()
    {
        using var fixture = new ToolDirectoryFixture();
        var resolver = new MediaToolResolver(fixture.ApplicationDirectory);

        var result = resolver.Resolve(MediaTool.Ffmpeg);

        Assert.False(result.IsAvailable);
        Assert.Null(result.ExecutablePath);
        Assert.Contains("ffmpeg.exe", result.ErrorMessage);
        Assert.Contains(resolver.ToolsDirectory, result.ErrorMessage);
    }

    [Theory]
    [InlineData(MediaTool.Ffmpeg, "-version")]
    [InlineData(MediaTool.Ffprobe, "-version")]
    [InlineData(MediaTool.YtDlp, "--version")]
    public async Task GetVersionAsync_UsesExpectedDiagnosticArgument(
        MediaTool tool,
        string expectedArgument)
    {
        using var fixture = new ToolDirectoryFixture();
        var executableName = tool switch
        {
            MediaTool.Ffmpeg => "ffmpeg.exe",
            MediaTool.Ffprobe => "ffprobe.exe",
            MediaTool.YtDlp => "yt-dlp.exe",
            _ => throw new ArgumentOutOfRangeException(nameof(tool))
        };
        fixture.CreateTool(tool == MediaTool.YtDlp ? "yt-dlp" : "ffmpeg", executableName);
        var processRunner = new StubProcessRunner(
            new ProcessRunResult(0, "version 1.2.3\r\nmore", string.Empty));
        var resolver = new MediaToolResolver(fixture.ApplicationDirectory, processRunner);

        var result = await resolver.GetVersionAsync(tool);

        Assert.True(result.IsAvailable);
        Assert.Equal("version 1.2.3", result.Version);
        Assert.Equal([expectedArgument], processRunner.LastArguments);
    }

    [Fact]
    public async Task GetDiagnosticsAsync_ReturnsAllToolsWithoutRunningMissingBinaries()
    {
        using var fixture = new ToolDirectoryFixture();
        var processRunner = new StubProcessRunner(
            new ProcessRunResult(0, "unused", string.Empty));
        var resolver = new MediaToolResolver(fixture.ApplicationDirectory, processRunner);

        var results = await resolver.GetDiagnosticsAsync();

        Assert.Equal(3, results.Count);
        Assert.All(results, result => Assert.False(result.IsAvailable));
        Assert.Equal(0, processRunner.CallCount);
    }

    private sealed class StubProcessRunner(ProcessRunResult result) : IProcessRunner
    {
        public int CallCount { get; private set; }

        public IReadOnlyList<string>? LastArguments { get; private set; }

        public Task<ProcessRunResult> RunAsync(
            string executablePath,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastArguments = arguments;
            return Task.FromResult(result);
        }
    }

    private sealed class ToolDirectoryFixture : IDisposable
    {
        public ToolDirectoryFixture()
        {
            ApplicationDirectory = Path.Combine(
                Path.GetTempPath(),
                "VideoToMp3.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ApplicationDirectory);
        }

        public string ApplicationDirectory { get; }

        public string CreateTool(string toolDirectory, string executableName)
        {
            var directory = Path.Combine(ApplicationDirectory, "tools", toolDirectory);
            Directory.CreateDirectory(directory);
            var executablePath = Path.Combine(directory, executableName);
            File.WriteAllText(executablePath, "test placeholder");
            return executablePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(ApplicationDirectory))
            {
                Directory.Delete(ApplicationDirectory, recursive: true);
            }
        }
    }
}
