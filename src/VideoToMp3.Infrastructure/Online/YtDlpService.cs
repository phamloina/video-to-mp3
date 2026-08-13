using System.Text.Json;
using System.Text.Json.Serialization;
using VideoToMp3.Core.Dependencies;
using VideoToMp3.Core.Online;
using VideoToMp3.Core.Models;
using VideoToMp3.Core.Services;
using VideoToMp3.Infrastructure.Processes;

namespace VideoToMp3.Infrastructure.Online;

public sealed class YtDlpService(
    IMediaToolResolver toolResolver,
    IProcessRunner processRunner) : IYtDlpService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<OnlineMediaProbeResult> ProbeAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedUrl(url))
        {
            return Failure(
                OnlineMediaProbeErrorCode.InvalidUrl,
                "URL phải sử dụng HTTP hoặc HTTPS.",
                url);
        }

        var ytDlp = toolResolver.Resolve(MediaTool.YtDlp);
        if (!ytDlp.IsAvailable || ytDlp.ExecutablePath is null)
        {
            return Failure(
                OnlineMediaProbeErrorCode.DependencyMissing,
                "Không tìm thấy yt-dlp. Hãy đặt yt-dlp.exe vào thư mục tools/yt-dlp.",
                ytDlp.ErrorMessage);
        }

        var arguments = new[]
        {
            "--dump-single-json",
            "--skip-download",
            "--no-warnings",
            "--playlist-items", "1",
            url
        };

        ProcessRunResult processResult;
        try
        {
            processResult = await processRunner
                .RunAsync(ytDlp.ExecutablePath, arguments, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return Failure(
                OnlineMediaProbeErrorCode.ProbeFailed,
                "Không thể chạy yt-dlp để phân tích URL.",
                exception.Message);
        }

        if (processResult.ExitCode != 0)
        {
            return ClassifyFailure(processResult.StandardError);
        }

        try
        {
            var document = JsonSerializer.Deserialize<YtDlpDocument>(
                processResult.StandardOutput,
                JsonOptions);
            if (document is null)
            {
                return Failure(
                    OnlineMediaProbeErrorCode.InvalidOutput,
                    "yt-dlp trả về dữ liệu JSON rỗng.");
            }

            var metadata = new MediaMetadata(
                Normalize(document.Title),
                Normalize(document.Artist),
                Normalize(document.Album),
                document.TrackNumber is > 0 ? document.TrackNumber : null);
            return OnlineMediaProbeResult.Success(
                metadata.Title,
                ParseDuration(document.Duration),
                ResolveThumbnail(document),
                Normalize(document.ExtractorKey) ?? Normalize(document.Extractor),
                string.Equals(document.Type, "playlist", StringComparison.OrdinalIgnoreCase),
                metadata);
        }
        catch (JsonException exception)
        {
            return Failure(
                OnlineMediaProbeErrorCode.InvalidOutput,
                "Dữ liệu JSON từ yt-dlp không hợp lệ.",
                exception.Message);
        }
    }

    public async Task<OnlineMediaDownloadResult> DownloadAsync(
        string url,
        string temporaryDirectory,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedUrl(url))
        {
            return DownloadFailure(OnlineMediaProbeErrorCode.InvalidUrl, "URL phải sử dụng HTTP hoặc HTTPS.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryDirectory);
        var ytDlp = toolResolver.Resolve(MediaTool.YtDlp);
        if (!ytDlp.IsAvailable || ytDlp.ExecutablePath is null)
        {
            return DownloadFailure(
                OnlineMediaProbeErrorCode.DependencyMissing,
                "Không tìm thấy yt-dlp.",
                ytDlp.ErrorMessage);
        }

        Directory.CreateDirectory(temporaryDirectory);
        var outputTemplate = Path.Combine(temporaryDirectory, "source.%(ext)s");
        var arguments = new[]
        {
            "--no-playlist", "--no-warnings", "--no-part", "--newline",
            "--progress-template", "download:%(progress._percent_str)s",
            "-f", "bestaudio/best",
            "-o", outputTemplate,
            url
        };

        try
        {
            ProcessRunResult result;
            if (progress is null)
            {
                result = await processRunner
                    .RunAsync(ytDlp.ExecutablePath, arguments, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var parser = new YtDlpProgressParser(progress);
                result = await processRunner
                    .RunWithStandardErrorProgressAsync(
                        ytDlp.ExecutablePath,
                        arguments,
                        new SynchronousProgress<string>(parser.Parse),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            if (result.ExitCode != 0)
            {
                return OnlineMediaDownloadResult.Failure(ClassifyFailure(result.StandardError).Error!);
            }

            var downloadedFile = Directory
                .EnumerateFiles(temporaryDirectory, "source.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path =>
                    !path.EndsWith(".part", StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase));
            return downloadedFile is null
                ? DownloadFailure(
                    OnlineMediaProbeErrorCode.InvalidOutput,
                    "yt-dlp hoàn tất nhưng không tạo file media.")
                : OnlineMediaDownloadResult.Success(downloadedFile);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return DownloadFailure(
                OnlineMediaProbeErrorCode.ProbeFailed,
                "Không thể tải media bằng yt-dlp.",
                exception.Message);
        }
    }

    private static bool IsSupportedUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static TimeSpan? ParseDuration(double? seconds) =>
        seconds is { } value && double.IsFinite(value) && value >= 0
            ? TimeSpan.FromSeconds(value)
            : null;

    private static string? ResolveThumbnail(YtDlpDocument document) =>
        Normalize(document.Thumbnail) ?? document.Thumbnails?
            .Select(item => Normalize(item.Url))
            .LastOrDefault(value => value is not null);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static OnlineMediaProbeResult ClassifyFailure(string standardError)
    {
        var details = standardError.Trim();
        if (details.Contains("Unsupported URL", StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                OnlineMediaProbeErrorCode.UnsupportedUrl,
                "Nguồn URL này không được yt-dlp hỗ trợ.",
                details);
        }

        if (details.Contains("login", StringComparison.OrdinalIgnoreCase) ||
            details.Contains("cookies", StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                OnlineMediaProbeErrorCode.AuthenticationRequired,
                "Nội dung yêu cầu đăng nhập hoặc cookie và hiện chưa được hỗ trợ.",
                details);
        }

        if (details.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
            details.Contains("private video", StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                OnlineMediaProbeErrorCode.MediaUnavailable,
                "Video không khả dụng hoặc không thể truy cập.",
                details);
        }

        return Failure(
            OnlineMediaProbeErrorCode.ProbeFailed,
            "yt-dlp không thể phân tích URL.",
            details);
    }

    private static OnlineMediaProbeResult Failure(
        OnlineMediaProbeErrorCode code,
        string message,
        string? details = null) =>
        OnlineMediaProbeResult.Failure(new OnlineMediaProbeError(code, message, details));

    private static OnlineMediaDownloadResult DownloadFailure(
        OnlineMediaProbeErrorCode code,
        string message,
        string? details = null) =>
        OnlineMediaDownloadResult.Failure(new OnlineMediaProbeError(code, message, details));

    private sealed class YtDlpDocument
    {
        [JsonPropertyName("_type")]
        public string? Type { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("duration")]
        public double? Duration { get; init; }

        [JsonPropertyName("artist")]
        public string? Artist { get; init; }

        [JsonPropertyName("album")]
        public string? Album { get; init; }

        [JsonPropertyName("track_number")]
        public int? TrackNumber { get; init; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; init; }

        [JsonPropertyName("extractor")]
        public string? Extractor { get; init; }

        [JsonPropertyName("extractor_key")]
        public string? ExtractorKey { get; init; }

        [JsonPropertyName("thumbnails")]
        public List<YtDlpThumbnail>? Thumbnails { get; init; }
    }

    private sealed class YtDlpThumbnail
    {
        [JsonPropertyName("url")]
        public string? Url { get; init; }
    }

    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
