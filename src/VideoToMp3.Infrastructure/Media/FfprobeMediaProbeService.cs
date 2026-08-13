using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using VideoToMp3.Core.Dependencies;
using VideoToMp3.Core.Media;
using VideoToMp3.Core.Models;
using VideoToMp3.Core.Services;
using VideoToMp3.Infrastructure.Processes;

namespace VideoToMp3.Infrastructure.Media;

public sealed class FfprobeMediaProbeService(
    IMediaToolResolver toolResolver,
    IProcessRunner processRunner) : IMediaProbeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<MediaProbeResult> ProbeAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return MediaProbeResult.Failure(new MediaProbeError(
                MediaProbeErrorCode.FileNotFound,
                "Không tìm thấy file media.",
                filePath));
        }

        var ffprobe = toolResolver.Resolve(MediaTool.Ffprobe);
        if (!ffprobe.IsAvailable || ffprobe.ExecutablePath is null)
        {
            return MediaProbeResult.Failure(new MediaProbeError(
                MediaProbeErrorCode.DependencyMissing,
                "Không tìm thấy ffprobe. Hãy đặt ffprobe.exe vào thư mục tools/ffmpeg.",
                ffprobe.ErrorMessage));
        }

        var arguments = new[]
        {
            "-v", "error",
            "-print_format", "json",
            "-show_format",
            "-show_streams",
            filePath
        };

        ProcessRunResult processResult;
        try
        {
            processResult = await processRunner
                .RunAsync(ffprobe.ExecutablePath, arguments, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return MediaProbeResult.Failure(new MediaProbeError(
                MediaProbeErrorCode.ProbeFailed,
                "Không thể chạy ffprobe để phân tích file.",
                exception.Message));
        }

        if (processResult.ExitCode != 0)
        {
            return MediaProbeResult.Failure(new MediaProbeError(
                MediaProbeErrorCode.ProbeFailed,
                "ffprobe không thể đọc file media.",
                processResult.StandardError.Trim()));
        }

        try
        {
            var document = JsonSerializer.Deserialize<FfprobeDocument>(
                processResult.StandardOutput,
                JsonOptions);

            if (document is null)
            {
                return InvalidOutput("ffprobe trả về JSON rỗng.");
            }

            var streams = document.Streams ?? [];
            var hasAudioStream = streams.Any(stream =>
                string.Equals(stream.CodecType, "audio", StringComparison.OrdinalIgnoreCase));
            var duration = ParseDuration(document.Format?.Duration)
                ?? streams.Select(stream => ParseDuration(stream.Duration))
                    .FirstOrDefault(value => value.HasValue);

            var tags = document.Format?.Tags;
            var metadata = new MediaMetadata(
                NormalizeOptionalText(tags?.Title),
                NormalizeOptionalText(tags?.Artist),
                NormalizeOptionalText(tags?.Album),
                ParseTrackNumber(tags?.Track));
            return MediaProbeResult.Success(
                duration,
                hasAudioStream,
                document.Format?.FormatName,
                metadata.Title,
                metadata);
        }
        catch (JsonException exception)
        {
            return InvalidOutput(exception.Message);
        }
    }

    private static MediaProbeResult InvalidOutput(string details) =>
        MediaProbeResult.Failure(new MediaProbeError(
            MediaProbeErrorCode.InvalidOutput,
            "Dữ liệu ffprobe trả về không hợp lệ.",
            details));

    private static TimeSpan? ParseDuration(string? value)
    {
        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var seconds) &&
            double.IsFinite(seconds) &&
            seconds >= 0
                ? TimeSpan.FromSeconds(seconds)
                : null;
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? ParseTrackNumber(string? value)
    {
        var firstPart = value?.Split('/', 2)[0].Trim();
        return int.TryParse(firstPart, out var track) && track > 0 ? track : null;
    }

    private sealed class FfprobeDocument
    {
        [JsonPropertyName("streams")]
        public List<FfprobeStream>? Streams { get; init; }

        [JsonPropertyName("format")]
        public FfprobeFormat? Format { get; init; }
    }

    private sealed class FfprobeStream
    {
        [JsonPropertyName("codec_type")]
        public string? CodecType { get; init; }

        [JsonPropertyName("duration")]
        public string? Duration { get; init; }
    }

    private sealed class FfprobeFormat
    {
        [JsonPropertyName("format_name")]
        public string? FormatName { get; init; }

        [JsonPropertyName("duration")]
        public string? Duration { get; init; }

        [JsonPropertyName("tags")]
        public FfprobeTags? Tags { get; init; }
    }

    private sealed class FfprobeTags
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("artist")]
        public string? Artist { get; init; }

        [JsonPropertyName("album")]
        public string? Album { get; init; }

        [JsonPropertyName("track")]
        public string? Track { get; init; }
    }
}
