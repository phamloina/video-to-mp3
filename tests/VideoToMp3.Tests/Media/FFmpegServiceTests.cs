using VideoToMp3.Core.Dependencies;
using VideoToMp3.Core.Models;
using VideoToMp3.Core.Services;
using VideoToMp3.Infrastructure.Media;
using VideoToMp3.Infrastructure.Processes;

namespace VideoToMp3.Tests.Media;

public sealed class FFmpegServiceTests
{
    [Fact]
    public async Task ConvertLocalToMp3Async_ConvertsRealSampleWhenFfmpegIsProvided()
    {
        var ffmpegPath = Environment.GetEnvironmentVariable("VIDEO_TO_MP3_TEST_FFMPEG");
        if (string.IsNullOrWhiteSpace(ffmpegPath))
        {
            return;
        }

        using var fixture = new ConversionFixture(createInput: false);
        var processRunner = new ProcessRunner();
        var generated = await processRunner.RunAsync(
            ffmpegPath,
            [
                "-hide_banner", "-loglevel", "error", "-y",
                "-f", "lavfi", "-i", "color=c=blue:s=160x120:d=1",
                "-f", "lavfi", "-i", "sine=frequency=1000:duration=1",
                "-shortest", fixture.InputPath
            ]);
        Assert.Equal(0, generated.ExitCode);

        var service = new FFmpegService(
            new StubMediaToolResolver(new MediaToolInfo(
                MediaTool.Ffmpeg,
                "ffmpeg.exe",
                ffmpegPath,
                true,
                null,
                null)),
            new OutputPathResolver(),
            processRunner);

        var result = await service.ConvertLocalToMp3Async(fixture.CreateJob(192));

        Assert.True(result.IsSuccess, result.TechnicalDetails);
        Assert.True(new FileInfo(result.OutputFilePath!).Length > 0);
    }

    [Fact]
    public async Task ConvertLocalToMp3Async_UsesBitrateAndNoOverwriteArguments()
    {
        using var fixture = new ConversionFixture();
        var runner = new StubProcessRunner(createOutput: true);
        var service = fixture.CreateService(runner);
        var job = fixture.CreateJob(256);

        var result = await service.ConvertLocalToMp3Async(job);

        Assert.True(result.IsSuccess);
        Assert.Equal(result.OutputFilePath, job.OutputFilePath);
        Assert.Contains("-n", runner.LastArguments!);
        Assert.Contains("-vn", runner.LastArguments!);
        Assert.Contains("256k", runner.LastArguments!);
        Assert.Contains("libmp3lame", runner.LastArguments!);
    }

    [Fact]
    public async Task ConvertLocalToMp3Async_ReturnsStderrWhenFfmpegFails()
    {
        using var fixture = new ConversionFixture();
        var runner = new StubProcessRunner(1, "encoder failed");
        var service = fixture.CreateService(runner);

        var result = await service.ConvertLocalToMp3Async(fixture.CreateJob());

        Assert.False(result.IsSuccess);
        Assert.Contains("encoder failed", result.TechnicalDetails);
    }

    [Fact]
    public async Task ConvertLocalToMp3Async_ReportsMissingFfmpegWithoutStartingProcess()
    {
        using var fixture = new ConversionFixture();
        var runner = new StubProcessRunner();
        var service = new FFmpegService(
            new StubMediaToolResolver(new MediaToolInfo(
                MediaTool.Ffmpeg,
                "ffmpeg.exe",
                null,
                false,
                null,
                "FFmpeg is missing")),
            new OutputPathResolver(),
            runner);

        var result = await service.ConvertLocalToMp3Async(fixture.CreateJob());

        Assert.False(result.IsSuccess);
        Assert.Equal(0, runner.CallCount);
    }

    private sealed class ConversionFixture : IDisposable
    {
        public ConversionFixture(bool createInput = true)
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                "VideoToMp3.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
            InputPath = Path.Combine(DirectoryPath, "sample video.mp4");
            if (createInput)
            {
                File.WriteAllBytes(InputPath, [0]);
            }
        }

        public string DirectoryPath { get; }
        public string InputPath { get; }

        public ConversionJob CreateJob(int bitrate = 320) =>
            new(ConversionSourceType.LocalFile, InputPath, DirectoryPath, bitrate);

        public FFmpegService CreateService(IProcessRunner runner) =>
            new(
                new StubMediaToolResolver(new MediaToolInfo(
                    MediaTool.Ffmpeg,
                    "ffmpeg.exe",
                    @"C:\tools\ffmpeg.exe",
                    true,
                    null,
                    null)),
                new OutputPathResolver(),
                runner);

        public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
    }

    private sealed class StubProcessRunner(
        int exitCode = 0,
        string standardError = "",
        bool createOutput = false) : IProcessRunner
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
            if (createOutput)
            {
                File.WriteAllText(arguments[^1], "mp3");
            }

            return Task.FromResult(new ProcessRunResult(exitCode, "", standardError));
        }
    }

    private sealed class StubMediaToolResolver(MediaToolInfo ffmpeg) : IMediaToolResolver
    {
        public string ToolsDirectory => @"C:\tools";
        public MediaToolInfo Resolve(MediaTool tool) => ffmpeg;
        public Task<MediaToolInfo> GetVersionAsync(MediaTool tool, CancellationToken cancellationToken = default) =>
            Task.FromResult(ffmpeg);
        public Task<IReadOnlyList<MediaToolInfo>> GetDiagnosticsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MediaToolInfo>>([ffmpeg]);
    }
}
