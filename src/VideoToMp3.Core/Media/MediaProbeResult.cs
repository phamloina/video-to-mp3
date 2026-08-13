namespace VideoToMp3.Core.Media;

using VideoToMp3.Core.Models;

public sealed record MediaProbeResult(
    bool IsSuccess,
    TimeSpan? Duration,
    bool HasAudioStream,
    string? Container,
    string? Title,
    MediaMetadata? Metadata,
    MediaProbeError? Error)
{
    public static MediaProbeResult Success(
        TimeSpan? duration,
        bool hasAudioStream,
        string? container,
        string? title,
        MediaMetadata? metadata = null) =>
        new(true, duration, hasAudioStream, container, title, metadata, null);

    public static MediaProbeResult Failure(MediaProbeError error) =>
        new(false, null, false, null, null, null, error);
}
