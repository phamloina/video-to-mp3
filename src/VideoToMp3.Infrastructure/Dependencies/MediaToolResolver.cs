using System.IO.Compression;
using VideoToMp3.Core.Dependencies;
using VideoToMp3.Core.Services;
using VideoToMp3.Infrastructure.Processes;

namespace VideoToMp3.Infrastructure.Dependencies;

public sealed class MediaToolResolver : IMediaToolResolver
{
    private const string YtDlpDownloadUrl =
        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
    private const string FfmpegDownloadUrl =
        "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

    private static readonly SemaphoreSlim YtDlpInstallLock = new(1, 1);
    private static readonly SemaphoreSlim FfmpegInstallLock = new(1, 1);
    private static readonly HttpClient SharedHttpClient = CreateHttpClient();

    private static readonly IReadOnlyDictionary<MediaTool, string> ExecutableNames =
        new Dictionary<MediaTool, string>
        {
            [MediaTool.Ffmpeg] = "ffmpeg.exe",
            [MediaTool.Ffprobe] = "ffprobe.exe",
            [MediaTool.YtDlp] = "yt-dlp.exe"
        };

    private readonly IProcessRunner _processRunner;
    private readonly IReadOnlyList<string> _pathDirectories;
    private readonly string _userToolsDirectory;
    private readonly string _wingetPackagesDirectory;
    private readonly HttpClient _httpClient;

    public MediaToolResolver(
        string applicationDirectory,
        IProcessRunner? processRunner = null,
        string? pathEnvironment = null,
        string? userToolsDirectory = null,
        string? wingetPackagesDirectory = null,
        HttpClient? httpClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);

        ToolsDirectory = Path.Combine(
            Path.GetFullPath(applicationDirectory),
            "tools");
        _processRunner = processRunner ?? new ProcessRunner();
        _pathDirectories = ParsePathDirectories(
            pathEnvironment ?? Environment.GetEnvironmentVariable("PATH"));
        _userToolsDirectory = Path.GetFullPath(userToolsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoToMp3",
            "tools"));
        _wingetPackagesDirectory = Path.GetFullPath(wingetPackagesDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WinGet",
            "Packages"));
        _httpClient = httpClient ?? SharedHttpClient;
    }

    public string ToolsDirectory { get; }

    public MediaToolInfo Resolve(MediaTool tool)
    {
        var executableName = GetExecutableName(tool);
        var managedPath = Path.Combine(
            ToolsDirectory,
            GetToolDirectoryName(tool),
            executableName);

        if (File.Exists(managedPath))
        {
            return Available(tool, executableName, managedPath);
        }

        var flatToolsPath = Path.Combine(ToolsDirectory, executableName);
        if (File.Exists(flatToolsPath))
        {
            return Available(tool, executableName, flatToolsPath);
        }

        var userManagedPath = Path.Combine(
            _userToolsDirectory,
            GetToolDirectoryName(tool),
            executableName);
        if (File.Exists(userManagedPath))
        {
            return Available(tool, executableName, userManagedPath);
        }

        var flatUserToolsPath = Path.Combine(_userToolsDirectory, executableName);
        if (File.Exists(flatUserToolsPath))
        {
            return Available(tool, executableName, flatUserToolsPath);
        }

        foreach (var directory in _pathDirectories)
        {
            var pathExecutable = Path.Combine(directory, executableName);
            if (File.Exists(pathExecutable))
            {
                return Available(tool, executableName, pathExecutable);
            }
        }

        var wingetExecutable = FindWingetExecutable(executableName);
        if (wingetExecutable is not null)
        {
            return Available(tool, executableName, wingetExecutable);
        }

        return new MediaToolInfo(
            tool,
            executableName,
            null,
            false,
            null,
            $"Không tìm thấy {executableName} trong thư mục {ToolsDirectory}, {_userToolsDirectory} hoặc PATH.");
    }

    public async Task<MediaToolInfo> EnsureAvailableAsync(
        MediaTool tool,
        CancellationToken cancellationToken = default)
    {
        var resolved = Resolve(tool);
        if (resolved.IsAvailable)
        {
            return resolved;
        }

        var installLock = tool == MediaTool.YtDlp
            ? YtDlpInstallLock
            : FfmpegInstallLock;
        await installLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            resolved = Resolve(tool);
            if (resolved.IsAvailable)
            {
                return resolved;
            }

            if (tool == MediaTool.YtDlp)
            {
                await InstallYtDlpAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await InstallFfmpegAsync(cancellationToken).ConfigureAwait(false);
            }

            return Resolve(tool);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            return resolved with
            {
                ErrorMessage = $"Không thể tự tải {resolved.ExecutableName}: {exception.Message}"
            };
        }
        finally
        {
            installLock.Release();
        }
    }

    public async Task<MediaToolInfo> GetVersionAsync(
        MediaTool tool,
        CancellationToken cancellationToken = default)
    {
        var resolved = Resolve(tool);
        if (!resolved.IsAvailable || resolved.ExecutablePath is null)
        {
            return resolved;
        }

        try
        {
            var arguments = tool == MediaTool.YtDlp
                ? new[] { "--version" }
                : new[] { "-version" };
            var result = await _processRunner
                .RunAsync(resolved.ExecutablePath, arguments, cancellationToken)
                .ConfigureAwait(false);
            var version = FirstNonEmptyLine(result.StandardOutput, result.StandardError);

            if (result.ExitCode != 0)
            {
                return resolved with
                {
                    IsAvailable = false,
                    ErrorMessage = $"{resolved.ExecutableName} trả về mã lỗi {result.ExitCode}: {version}"
                };
            }

            return resolved with { Version = version };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return resolved with
            {
                IsAvailable = false,
                ErrorMessage = $"Không thể chạy {resolved.ExecutableName}: {exception.Message}"
            };
        }
    }

    public async Task<IReadOnlyList<MediaToolInfo>> GetDiagnosticsAsync(
        CancellationToken cancellationToken = default)
    {
        var diagnostics = new List<MediaToolInfo>();

        foreach (var tool in Enum.GetValues<MediaTool>())
        {
            diagnostics.Add(await GetVersionAsync(tool, cancellationToken).ConfigureAwait(false));
        }

        return diagnostics;
    }

    private static string GetExecutableName(MediaTool tool) =>
        ExecutableNames.TryGetValue(tool, out var executableName)
            ? executableName
            : throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unsupported media tool.");

    private static string GetToolDirectoryName(MediaTool tool) => tool switch
    {
        MediaTool.Ffmpeg or MediaTool.Ffprobe => "ffmpeg",
        MediaTool.YtDlp => "yt-dlp",
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unsupported media tool.")
    };

    private static MediaToolInfo Available(
        MediaTool tool,
        string executableName,
        string executablePath) =>
        new(tool, executableName, Path.GetFullPath(executablePath), true, null, null);

    private static IReadOnlyList<string> ParsePathDirectories(string? pathEnvironment)
    {
        if (string.IsNullOrWhiteSpace(pathEnvironment))
        {
            return [];
        }

        return pathEnvironment
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(directory => directory.Trim('"'))
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string? FindWingetExecutable(string executableName)
    {
        if (!Directory.Exists(_wingetPackagesDirectory))
        {
            return null;
        }

        try
        {
            return Directory
                .EnumerateFiles(_wingetPackagesDirectory, executableName, SearchOption.AllDirectories)
                .FirstOrDefault();
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private async Task InstallYtDlpAsync(CancellationToken cancellationToken)
    {
        var destinationDirectory = Path.Combine(_userToolsDirectory, "yt-dlp");
        Directory.CreateDirectory(destinationDirectory);
        var destinationPath = Path.Combine(destinationDirectory, "yt-dlp.exe");
        var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.download";
        try
        {
            await DownloadFileAsync(YtDlpDownloadUrl, temporaryPath, cancellationToken)
                .ConfigureAwait(false);
            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            TryDeleteFile(temporaryPath);
        }
    }

    private async Task InstallFfmpegAsync(CancellationToken cancellationToken)
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "VideoToMp3",
            $"tools-{Guid.NewGuid():N}");
        var archivePath = Path.Combine(temporaryDirectory, "ffmpeg.zip");
        var extractionDirectory = Path.Combine(temporaryDirectory, "extract");
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            await DownloadFileAsync(FfmpegDownloadUrl, archivePath, cancellationToken)
                .ConfigureAwait(false);
            ZipFile.ExtractToDirectory(archivePath, extractionDirectory);

            var ffmpeg = Directory
                .EnumerateFiles(extractionDirectory, "ffmpeg.exe", SearchOption.AllDirectories)
                .FirstOrDefault();
            var ffprobe = Directory
                .EnumerateFiles(extractionDirectory, "ffprobe.exe", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (ffmpeg is null || ffprobe is null)
            {
                throw new InvalidDataException("Gói FFmpeg không chứa ffmpeg.exe và ffprobe.exe.");
            }

            var destinationDirectory = Path.Combine(_userToolsDirectory, "ffmpeg");
            Directory.CreateDirectory(destinationDirectory);
            File.Copy(ffmpeg, Path.Combine(destinationDirectory, "ffmpeg.exe"), overwrite: true);
            File.Copy(ffprobe, Path.Combine(destinationDirectory, "ffprobe.exe"), overwrite: true);
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    private async Task DownloadFileAsync(
        string url,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var output = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);
        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VideoToMp3/0.2.1");
        return client;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static string? FirstNonEmptyLine(params string[] values)
    {
        return values
            .SelectMany(value => value.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries))
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0);
    }
}
