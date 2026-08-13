using VideoToMp3.Core.Dependencies;
using VideoToMp3.Core.Services;
using VideoToMp3.Infrastructure.Processes;

namespace VideoToMp3.Infrastructure.Dependencies;

public sealed class MediaToolResolver : IMediaToolResolver
{
    private static readonly IReadOnlyDictionary<MediaTool, string> ExecutableNames =
        new Dictionary<MediaTool, string>
        {
            [MediaTool.Ffmpeg] = "ffmpeg.exe",
            [MediaTool.Ffprobe] = "ffprobe.exe",
            [MediaTool.YtDlp] = "yt-dlp.exe"
        };

    private readonly IProcessRunner _processRunner;
    private readonly IReadOnlyList<string> _pathDirectories;

    public MediaToolResolver(
        string applicationDirectory,
        IProcessRunner? processRunner = null,
        string? pathEnvironment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);

        ToolsDirectory = Path.Combine(
            Path.GetFullPath(applicationDirectory),
            "tools");
        _processRunner = processRunner ?? new ProcessRunner();
        _pathDirectories = ParsePathDirectories(
            pathEnvironment ?? Environment.GetEnvironmentVariable("PATH"));
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

        foreach (var directory in _pathDirectories)
        {
            var pathExecutable = Path.Combine(directory, executableName);
            if (File.Exists(pathExecutable))
            {
                return Available(tool, executableName, pathExecutable);
            }
        }

        return new MediaToolInfo(
            tool,
            executableName,
            null,
            false,
            null,
            $"Không tìm thấy {executableName} trong thư mục {ToolsDirectory} hoặc PATH.");
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

    private static string? FirstNonEmptyLine(params string[] values)
    {
        return values
            .SelectMany(value => value.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries))
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0);
    }
}
