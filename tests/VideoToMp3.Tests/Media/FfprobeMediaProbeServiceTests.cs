using VideoToMp3.Core.Dependencies;
using VideoToMp3.Core.Media;
using VideoToMp3.Core.Services;
using VideoToMp3.Infrastructure.Media;
using VideoToMp3.Infrastructure.Processes;

namespace VideoToMp3.Tests.Media;

public sealed class FfprobeMediaProbeServiceTests
{
    [Fact]
    public async Task ProbeAsync_ReadsDurationAudioContainerAndTitleFromJson()
    {
        using var mediaFile = new TemporaryMediaFile();
        const string json = """
            {
              "streams": [
                { "codec_type": "video", "duration": "12.5" },
                { "codec_type": "audio", "duration": "12.4" }
              ],
              "format": {
                "format_name": "mov,mp4,m4a,3gp,3g2,mj2",
                "duration": "12.500000",
                "tags": { "title": "Sample title" }
              }
            }
            """;
        var runner = new StubProcessRunner(new ProcessRunResult(0, json, string.Empty));
        var service = CreateService(runner);

        var result = await service.ProbeAsync(mediaFile.Path);

        Assert.True(result.IsSuccess);
        Assert.Equal(TimeSpan.FromSeconds(12.5), result.Duration);
        Assert.True(result.HasAudioStream);
        Assert.Equal("mov,mp4,m4a,3gp,3g2,mj2", result.Container);
        Assert.Equal("Sample title", result.Title);
        var arguments = Assert.IsAssignableFrom<IReadOnlyList<string>>(runner.LastArguments);
        Assert.Contains("-print_format", arguments);
        Assert.Contains("json", arguments);
        Assert.Equal(mediaFile.Path, arguments.Last());
    }

    [Fact]
    public async Task ProbeAsync_IdentifiesFileWithoutAudioStream()
    {
        using var mediaFile = new TemporaryMediaFile();
        const string json = """
            {
              "streams": [{ "codec_type": "video" }],
              "format": { "format_name": "matroska,webm", "duration": "4.25" }
            }
            """;
        var service = CreateService(
            new StubProcessRunner(new ProcessRunResult(0, json, string.Empty)));

        var result = await service.ProbeAsync(mediaFile.Path);

        Assert.True(result.IsSuccess);
        Assert.False(result.HasAudioStream);
        Assert.Equal(TimeSpan.FromSeconds(4.25), result.Duration);
    }

    [Fact]
    public async Task ProbeAsync_ReturnsStructuredErrorForMissingFile()
    {
        var runner = new StubProcessRunner(new ProcessRunResult(0, "{}", string.Empty));
        var service = CreateService(runner);

        var result = await service.ProbeAsync(@"C:\missing\video.mp4");

        Assert.False(result.IsSuccess);
        Assert.Equal(MediaProbeErrorCode.FileNotFound, result.Error?.Code);
        Assert.Equal(0, runner.CallCount);
    }

    [Fact]
    public async Task ProbeAsync_ReturnsStructuredErrorWhenFfprobeFails()
    {
        using var mediaFile = new TemporaryMediaFile();
        var service = CreateService(
            new StubProcessRunner(new ProcessRunResult(1, string.Empty, "Invalid data found")));

        var result = await service.ProbeAsync(mediaFile.Path);

        Assert.False(result.IsSuccess);
        Assert.Equal(MediaProbeErrorCode.ProbeFailed, result.Error?.Code);
        Assert.Contains("Invalid data found", result.Error?.TechnicalDetails);
    }

    [Fact]
    public async Task ProbeAsync_ReturnsStructuredErrorForInvalidJson()
    {
        using var mediaFile = new TemporaryMediaFile();
        var service = CreateService(
            new StubProcessRunner(new ProcessRunResult(0, "not-json", string.Empty)));

        var result = await service.ProbeAsync(mediaFile.Path);

        Assert.False(result.IsSuccess);
        Assert.Equal(MediaProbeErrorCode.InvalidOutput, result.Error?.Code);
    }

    private static FfprobeMediaProbeService CreateService(IProcessRunner processRunner)
    {
        var ffprobe = new MediaToolInfo(
            MediaTool.Ffprobe,
            "ffprobe.exe",
            @"C:\tools\ffprobe.exe",
            true,
            null,
            null);

        return new FfprobeMediaProbeService(
            new StubMediaToolResolver(ffprobe),
            processRunner);
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

    private sealed class StubMediaToolResolver(MediaToolInfo ffprobe) : IMediaToolResolver
    {
        public string ToolsDirectory => @"C:\tools";

        public MediaToolInfo Resolve(MediaTool tool) => tool == MediaTool.Ffprobe
            ? ffprobe
            : throw new ArgumentOutOfRangeException(nameof(tool));

        public Task<MediaToolInfo> GetVersionAsync(
            MediaTool tool,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Resolve(tool));

        public Task<IReadOnlyList<MediaToolInfo>> GetDiagnosticsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MediaToolInfo>>([ffprobe]);
    }

    private sealed class TemporaryMediaFile : IDisposable
    {
        public TemporaryMediaFile()
        {
            var directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "VideoToMp3.Tests");
            Directory.CreateDirectory(directory);
            Path = System.IO.Path.Combine(directory, $"{Guid.NewGuid():N}.mp4");
            File.WriteAllBytes(Path, [0]);
        }

        public string Path { get; }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}
