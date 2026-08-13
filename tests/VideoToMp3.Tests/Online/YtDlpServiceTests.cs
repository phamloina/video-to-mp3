using VideoToMp3.Core.Dependencies;
using VideoToMp3.Core.Online;
using VideoToMp3.Core.Services;
using VideoToMp3.Infrastructure.Online;
using VideoToMp3.Infrastructure.Processes;

namespace VideoToMp3.Tests.Online;

public sealed class YtDlpServiceTests
{
    [Fact]
    public async Task ProbeAsync_ReadsVideoMetadataFromJson()
    {
        const string json = """
            {
              "_type": "video",
              "title": "Sample video",
              "artist": "Sample artist",
              "album": "Sample album",
              "track_number": 4,
              "duration": 125.5,
              "thumbnail": "https://cdn.example.com/thumb.jpg",
              "extractor": "example",
              "extractor_key": "ExampleVideo"
            }
            """;
        var runner = new StubProcessRunner(new ProcessRunResult(0, json, ""));
        var service = CreateService(runner);

        var result = await service.ProbeAsync("https://example.com/watch?v=abc");

        Assert.True(result.IsSuccess);
        Assert.Equal("Sample video", result.Title);
        Assert.Equal("Sample artist", result.Metadata?.Artist);
        Assert.Equal("Sample album", result.Metadata?.Album);
        Assert.Equal(4, result.Metadata?.TrackNumber);
        Assert.Equal(TimeSpan.FromSeconds(125.5), result.Duration);
        Assert.Equal("https://cdn.example.com/thumb.jpg", result.ThumbnailUrl);
        Assert.Equal("ExampleVideo", result.Extractor);
        Assert.False(result.IsPlaylist);
        Assert.Contains("--dump-single-json", runner.LastArguments!);
        Assert.Contains("--skip-download", runner.LastArguments!);
        Assert.Equal("https://example.com/watch?v=abc", runner.LastArguments![^1]);
    }

    [Fact]
    public async Task ProbeAsync_IdentifiesPlaylistWithoutEnumeratingAllItems()
    {
        const string json = """
            {
              "_type": "playlist",
              "title": "Sample playlist",
              "extractor_key": "ExamplePlaylist",
              "thumbnails": [
                { "url": "https://cdn.example.com/small.jpg" },
                { "url": "https://cdn.example.com/large.jpg" }
              ]
            }
            """;
        var runner = new StubProcessRunner(new ProcessRunResult(0, json, ""));
        var service = CreateService(runner);

        var result = await service.ProbeAsync("https://example.com/playlist/1");

        Assert.True(result.IsSuccess);
        Assert.True(result.IsPlaylist);
        Assert.Equal("https://cdn.example.com/large.jpg", result.ThumbnailUrl);
        var arguments = Assert.IsAssignableFrom<IReadOnlyList<string>>(runner.LastArguments);
        var itemArgument = Array.IndexOf(arguments.ToArray(), "--playlist-items");
        Assert.True(itemArgument >= 0);
        Assert.Equal("1", arguments[itemArgument + 1]);
    }

    [Fact]
    public async Task ProbeSingleAsync_DisablesPlaylistExpansion()
    {
        var runner = new StubProcessRunner(new ProcessRunResult(0, "{\"_type\":\"video\"}", ""));

        var result = await CreateService(runner).ProbeSingleAsync(
            "https://example.com/watch?v=1&list=abc");

        Assert.True(result.IsSuccess);
        Assert.Contains("--no-playlist", runner.LastArguments!);
        Assert.DoesNotContain("--playlist-items", runner.LastArguments!);
    }

    [Fact]
    public async Task ExpandPlaylistAsync_ReturnsDistinctEntriesAndAppliesSafeLimit()
    {
        const string json = """
            {
              "_type": "playlist",
              "title": "Sample playlist",
              "entries": [
                {"webpage_url":"https://example.com/1","title":"One","duration":10},
                {"webpage_url":"https://example.com/2","title":"Two","duration":20},
                {"webpage_url":"https://example.com/3","title":"Three","duration":30}
              ]
            }
            """;
        var runner = new StubProcessRunner(new ProcessRunResult(0, json, ""));

        var result = await CreateService(runner).ExpandPlaylistAsync(
            "https://example.com/playlist",
            maximumItems: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal("Sample playlist", result.Title);
        Assert.Equal(2, result.Entries.Count);
        Assert.True(result.WasLimited);
        Assert.Equal("https://example.com/1", result.Entries[0].Url);
        Assert.Equal(TimeSpan.FromSeconds(20), result.Entries[1].Duration);
        var arguments = Assert.IsAssignableFrom<IReadOnlyList<string>>(runner.LastArguments);
        Assert.Contains("--flat-playlist", arguments);
        var endIndex = arguments.ToList().IndexOf("--playlist-end");
        Assert.Equal("3", arguments[endIndex + 1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("file:///C:/video.mp4")]
    [InlineData("ftp://example.com/video")]
    public async Task ProbeAsync_RejectsInvalidOrUnsafeUrlSchemes(string url)
    {
        var runner = new StubProcessRunner(new ProcessRunResult(0, "{}", ""));
        var result = await CreateService(runner).ProbeAsync(url);

        Assert.False(result.IsSuccess);
        Assert.Equal(OnlineMediaProbeErrorCode.InvalidUrl, result.Error?.Code);
        Assert.Equal(0, runner.CallCount);
    }

    [Fact]
    public async Task ProbeAsync_ReturnsDependencyMissingWithoutStartingProcess()
    {
        var runner = new StubProcessRunner(new ProcessRunResult(0, "{}", ""));
        var missingTool = new MediaToolInfo(
            MediaTool.YtDlp,
            "yt-dlp.exe",
            null,
            false,
            null,
            "missing");
        var service = new YtDlpService(new StubMediaToolResolver(missingTool), runner);

        var result = await service.ProbeAsync("https://example.com/video");

        Assert.False(result.IsSuccess);
        Assert.Equal(OnlineMediaProbeErrorCode.DependencyMissing, result.Error?.Code);
        Assert.Equal(0, runner.CallCount);
    }

    [Theory]
    [InlineData("ERROR: Unsupported URL", OnlineMediaProbeErrorCode.UnsupportedUrl)]
    [InlineData("Video unavailable", OnlineMediaProbeErrorCode.MediaUnavailable)]
    [InlineData("Sign in to confirm; use cookies", OnlineMediaProbeErrorCode.AuthenticationRequired)]
    [InlineData("Network error", OnlineMediaProbeErrorCode.ProbeFailed)]
    public async Task ProbeAsync_ClassifiesYtDlpFailures(
        string standardError,
        OnlineMediaProbeErrorCode expectedCode)
    {
        var service = CreateService(
            new StubProcessRunner(new ProcessRunResult(1, "", standardError)));

        var result = await service.ProbeAsync("https://example.com/video");

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error?.Code);
        Assert.Contains(standardError, result.Error?.TechnicalDetails);
    }

    [Fact]
    public async Task ProbeAsync_ReturnsStructuredErrorForInvalidJson()
    {
        var service = CreateService(
            new StubProcessRunner(new ProcessRunResult(0, "not-json", "")));

        var result = await service.ProbeAsync("https://example.com/video");

        Assert.False(result.IsSuccess);
        Assert.Equal(OnlineMediaProbeErrorCode.InvalidOutput, result.Error?.Code);
    }

    [Fact]
    public async Task DownloadAsync_DownloadsBestAudioIntoManagedDirectory()
    {
        using var directory = new TemporaryDirectory();
        var runner = new StubProcessRunner(
            new ProcessRunResult(0, "", ""),
            arguments => File.WriteAllText(
                Path.Combine(directory.Path, "source.webm"),
                "audio"));
        var service = CreateService(runner);

        var result = await service.DownloadAsync(
            "https://example.com/video",
            directory.Path);

        Assert.True(result.IsSuccess);
        Assert.Equal(Path.Combine(directory.Path, "source.webm"), result.DownloadedFilePath);
        Assert.Contains("bestaudio/best", runner.LastArguments!);
        Assert.Contains("--no-playlist", runner.LastArguments!);
        Assert.Contains("--no-part", runner.LastArguments!);
    }

    [Fact]
    public async Task DownloadAsync_FailsWhenYtDlpCreatesNoMediaFile()
    {
        using var directory = new TemporaryDirectory();
        var result = await CreateService(
            new StubProcessRunner(new ProcessRunResult(0, "", "")))
            .DownloadAsync("https://example.com/video", directory.Path);

        Assert.False(result.IsSuccess);
        Assert.Equal(OnlineMediaProbeErrorCode.InvalidOutput, result.Error?.Code);
    }

    [Fact]
    public async Task DownloadAsync_NetworkInterruptionReturnsStructuredFailure()
    {
        using var directory = new TemporaryDirectory();
        var result = await CreateService(new StubProcessRunner(
                new ProcessRunResult(1, "", "network unreachable")))
            .DownloadAsync("https://example.com/video", directory.Path);

        Assert.False(result.IsSuccess);
        Assert.Equal(OnlineMediaProbeErrorCode.ProbeFailed, result.Error?.Code);
        Assert.Contains("network unreachable", result.Error?.TechnicalDetails);
        Assert.Empty(Directory.EnumerateFiles(directory.Path));
    }

    [Fact]
    public async Task DownloadThumbnailAsync_UsesManagedJpegOutput()
    {
        using var directory = new TemporaryDirectory();
        var runner = new StubProcessRunner(
            new ProcessRunResult(0, "", ""),
            _ => File.WriteAllBytes(Path.Combine(directory.Path, "cover.jpg"), [1]));

        var result = await CreateService(runner).DownloadThumbnailAsync(
            "https://example.com/video",
            directory.Path);

        Assert.True(result.IsSuccess);
        Assert.Equal(Path.Combine(directory.Path, "cover.jpg"), result.ThumbnailFilePath);
        Assert.Contains("--skip-download", runner.LastArguments!);
        Assert.Contains("--write-thumbnail", runner.LastArguments!);
        Assert.Contains("--convert-thumbnails", runner.LastArguments!);
        Assert.Contains("jpg", runner.LastArguments!);
        Assert.Contains("--no-playlist", runner.LastArguments!);
    }

    private static YtDlpService CreateService(IProcessRunner runner)
    {
        var tool = new MediaToolInfo(
            MediaTool.YtDlp,
            "yt-dlp.exe",
            @"C:\tools\yt-dlp.exe",
            true,
            null,
            null);
        return new YtDlpService(new StubMediaToolResolver(tool), runner);
    }

    private sealed class StubProcessRunner(
        ProcessRunResult result,
        Action<IReadOnlyList<string>>? onRun = null) : IProcessRunner
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
            onRun?.Invoke(arguments);
            return Task.FromResult(result);
        }
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

    private sealed class StubMediaToolResolver(MediaToolInfo tool) : IMediaToolResolver
    {
        public string ToolsDirectory => @"C:\tools";
        public MediaToolInfo Resolve(MediaTool requestedTool) => tool;
        public Task<MediaToolInfo> GetVersionAsync(
            MediaTool requestedTool,
            CancellationToken cancellationToken = default) => Task.FromResult(tool);
        public Task<IReadOnlyList<MediaToolInfo>> GetDiagnosticsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MediaToolInfo>>([tool]);
    }
}
