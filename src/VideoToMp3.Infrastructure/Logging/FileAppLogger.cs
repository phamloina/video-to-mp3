using System.Text.RegularExpressions;
using VideoToMp3.Core.Services;

namespace VideoToMp3.Infrastructure.Logging;

public sealed partial class FileAppLogger : IAppLogger
{
    private const long MaximumLogBytes = 2 * 1024 * 1024;
    private readonly object _syncRoot = new();
    private readonly string _logFilePath;

    public FileAppLogger(string? logDirectory = null)
    {
        var directory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoToMp3",
            "logs");
        _logFilePath = Path.Combine(directory, "video-to-mp3.log");
    }

    public void LogError(Guid jobId, string userMessage, string? technicalDetails = null)
    {
        try
        {
            lock (_syncRoot)
            {
                var directory = Path.GetDirectoryName(_logFilePath)!;
                Directory.CreateDirectory(directory);
                RotateIfNeeded();
                var detail = string.IsNullOrWhiteSpace(technicalDetails)
                    ? "(no technical details)"
                    : Redact(technicalDetails);
                var entry = $"{DateTimeOffset.UtcNow:O} [ERROR] Job={jobId:N} " +
                            $"Message={SingleLine(userMessage)} Detail={SingleLine(detail)}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, entry);
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Logging must never interrupt conversion or create extra user popups.
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logFilePath) || new FileInfo(_logFilePath).Length < MaximumLogBytes)
        {
            return;
        }

        var previous = $"{_logFilePath}.1";
        File.Move(_logFilePath, previous, overwrite: true);
    }

    private static string Redact(string value)
    {
        var redacted = SensitiveHeaderRegex().Replace(value, "$1[REDACTED]");
        return SensitiveQueryRegex().Replace(redacted, "$1[REDACTED]");
    }

    private static string SingleLine(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ').Trim();

    [GeneratedRegex("(?i)(authorization\\s*[:=]\\s*|cookie\\s*[:=]\\s*)[^\\r\\n]+")]
    private static partial Regex SensitiveHeaderRegex();

    [GeneratedRegex("(?i)([?&](?:token|access_token|auth|key|api_key|signature)=)[^&\\s]+")]
    private static partial Regex SensitiveQueryRegex();
}
