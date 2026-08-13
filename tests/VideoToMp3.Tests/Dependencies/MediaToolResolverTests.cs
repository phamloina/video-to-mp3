using System.IO.Compression;
using System.Net;
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
        var resolver = CreateIsolatedResolver(fixture);

        var result = resolver.Resolve(tool);

        Assert.True(result.IsAvailable);
        Assert.Equal(Path.GetFullPath(executablePath), result.ExecutablePath);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Resolve_UsesExecutableFromPathWhenManagedToolIsMissing()
    {
        using var fixture = new ToolDirectoryFixture();
        var pathDirectory = fixture.CreateDirectory("path-tools");
        var executablePath = Path.Combine(pathDirectory, "yt-dlp.exe");
        File.WriteAllText(executablePath, "test placeholder");
        var resolver = CreateIsolatedResolver(fixture, pathEnvironment: pathDirectory);

        var result = resolver.Resolve(MediaTool.YtDlp);

        Assert.True(result.IsAvailable);
        Assert.Equal(Path.GetFullPath(executablePath), result.ExecutablePath);
    }

    [Fact]
    public void Resolve_UsesAutomaticallyDownloadedUserTool()
    {
        using var fixture = new ToolDirectoryFixture();
        var userToolsDirectory = fixture.CreateDirectory("user-tools");
        var ytDlpDirectory = Path.Combine(userToolsDirectory, "yt-dlp");
        Directory.CreateDirectory(ytDlpDirectory);
        var executablePath = Path.Combine(ytDlpDirectory, "yt-dlp.exe");
        File.WriteAllText(executablePath, "test placeholder");
        var resolver = new MediaToolResolver(
            fixture.ApplicationDirectory,
            pathEnvironment: string.Empty,
            userToolsDirectory: userToolsDirectory,
            wingetPackagesDirectory: fixture.CreateDirectory("empty-winget"));

        var result = resolver.Resolve(MediaTool.YtDlp);

        Assert.True(result.IsAvailable);
        Assert.Equal(Path.GetFullPath(executablePath), result.ExecutablePath);
    }

    [Fact]
    public async Task EnsureAvailableAsync_DoesNotDownloadAnExistingTool()
    {
        using var fixture = new ToolDirectoryFixture();
        var executablePath = fixture.CreateTool("ffmpeg", "ffmpeg.exe");
        var resolver = new MediaToolResolver(
            fixture.ApplicationDirectory,
            pathEnvironment: string.Empty,
            userToolsDirectory: fixture.CreateDirectory("user-tools"),
            wingetPackagesDirectory: fixture.CreateDirectory("empty-winget"));

        var result = await resolver.EnsureAvailableAsync(MediaTool.Ffmpeg);

        Assert.True(result.IsAvailable);
        Assert.Equal(Path.GetFullPath(executablePath), result.ExecutablePath);
    }

    [Fact]
    public async Task EnsureAvailableAsync_DownloadsMissingYtDlpToUserTools()
    {
        using var fixture = new ToolDirectoryFixture();
        var userToolsDirectory = fixture.CreateDirectory("downloaded-user-tools");
        using var client = new HttpClient(new StubHttpMessageHandler([1, 2, 3]));
        var resolver = new MediaToolResolver(
            fixture.ApplicationDirectory,
            pathEnvironment: string.Empty,
            userToolsDirectory: userToolsDirectory,
            wingetPackagesDirectory: fixture.CreateDirectory("empty-winget"),
            httpClient: client);

        var result = await resolver.EnsureAvailableAsync(MediaTool.YtDlp);

        Assert.True(result.IsAvailable);
        Assert.Equal(
            Path.Combine(userToolsDirectory, "yt-dlp", "yt-dlp.exe"),
            result.ExecutablePath);
        Assert.Equal([1, 2, 3], File.ReadAllBytes(result.ExecutablePath!));
    }

    [Fact]
    public async Task EnsureAvailableAsync_DownloadsFfmpegPairFromArchive()
    {
        using var fixture = new ToolDirectoryFixture();
        var userToolsDirectory = fixture.CreateDirectory("downloaded-user-tools");
        using var client = new HttpClient(new StubHttpMessageHandler(CreateFfmpegArchive()));
        var resolver = new MediaToolResolver(
            fixture.ApplicationDirectory,
            pathEnvironment: string.Empty,
            userToolsDirectory: userToolsDirectory,
            wingetPackagesDirectory: fixture.CreateDirectory("empty-winget"),
            httpClient: client);

        var result = await resolver.EnsureAvailableAsync(MediaTool.Ffprobe);

        Assert.True(result.IsAvailable);
        Assert.True(File.Exists(Path.Combine(userToolsDirectory, "ffmpeg", "ffmpeg.exe")));
        Assert.True(File.Exists(Path.Combine(userToolsDirectory, "ffmpeg", "ffprobe.exe")));
    }

    [Fact]
    public void Resolve_ReturnsStructuredMissingDependencyResult()
    {
        using var fixture = new ToolDirectoryFixture();
        var resolver = CreateIsolatedResolver(fixture);

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
        var resolver = CreateIsolatedResolver(fixture, processRunner);

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
        var resolver = CreateIsolatedResolver(fixture, processRunner);

        var results = await resolver.GetDiagnosticsAsync();

        Assert.Equal(3, results.Count);
        Assert.All(results, result => Assert.False(result.IsAvailable));
        Assert.Equal(0, processRunner.CallCount);
    }

    private static MediaToolResolver CreateIsolatedResolver(
        ToolDirectoryFixture fixture,
        IProcessRunner? processRunner = null,
        string pathEnvironment = "") =>
        new(
            fixture.ApplicationDirectory,
            processRunner,
            pathEnvironment,
            fixture.CreateDirectory("isolated-user-tools"),
            fixture.CreateDirectory("isolated-winget"));

    private static byte[] CreateFfmpegArchive()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var fileName in new[] { "ffmpeg.exe", "ffprobe.exe" })
            {
                var entry = archive.CreateEntry($"ffmpeg/bin/{fileName}");
                using var output = entry.Open();
                output.Write([1, 2, 3]);
            }
        }
        return stream.ToArray();
    }

    private sealed class StubHttpMessageHandler(byte[] content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            });
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

        public string CreateDirectory(string directoryName)
        {
            var directory = Path.Combine(ApplicationDirectory, directoryName);
            Directory.CreateDirectory(directory);
            return directory;
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
