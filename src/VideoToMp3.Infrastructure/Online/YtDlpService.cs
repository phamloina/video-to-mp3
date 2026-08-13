using System.Text.Json;
using System.Text.Json.Serialization;
using VideoToMp3.Core.Dependencies;
using VideoToMp3.Core.Online;
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

            return OnlineMediaProbeResult.Success(
                Normalize(document.Title),
                ParseDuration(document.Duration),
                ResolveThumbnail(document),
                Normalize(document.ExtractorKey) ?? Normalize(document.Extractor),
                string.Equals(document.Type, "playlist", StringComparison.OrdinalIgnoreCase));
        }
        catch (JsonException exception)
        {
            return Failure(
                OnlineMediaProbeErrorCode.InvalidOutput,
                "Dữ liệu JSON từ yt-dlp không hợp lệ.",
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

    private sealed class YtDlpDocument
    {
        [JsonPropertyName("_type")]
        public string? Type { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("duration")]
        public double? Duration { get; init; }

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
}
