namespace VideoToMp3.Core.Media;

public sealed record MediaProbeResult(
    bool IsSuccess,
    TimeSpan? Duration,
    bool HasAudioStream,
    string? Container,
    string? Title,
    MediaProbeError? Error)
{
    public static MediaProbeResult Success(
        TimeSpan? duration,
        bool hasAudioStream,
        string? container,
        string? title) =>
        new(true, duration, hasAudioStream, container, title, null);

    public static MediaProbeResult Failure(MediaProbeError error) =>
        new(false, null, false, null, null, error);
}
