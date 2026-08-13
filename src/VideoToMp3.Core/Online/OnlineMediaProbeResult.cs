namespace VideoToMp3.Core.Online;

public sealed record OnlineMediaProbeResult(
    bool IsSuccess,
    string? Title,
    TimeSpan? Duration,
    string? ThumbnailUrl,
    string? Extractor,
    bool IsPlaylist,
    OnlineMediaProbeError? Error)
{
    public static OnlineMediaProbeResult Success(
        string? title,
        TimeSpan? duration,
        string? thumbnailUrl,
        string? extractor,
        bool isPlaylist) =>
        new(true, title, duration, thumbnailUrl, extractor, isPlaylist, null);

    public static OnlineMediaProbeResult Failure(OnlineMediaProbeError error) =>
        new(false, null, null, null, null, false, error);
}
