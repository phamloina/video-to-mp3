namespace VideoToMp3.Core.Online;

using VideoToMp3.Core.Models;

public sealed record OnlineMediaProbeResult(
    bool IsSuccess,
    string? Title,
    TimeSpan? Duration,
    string? ThumbnailUrl,
    string? Extractor,
    bool IsPlaylist,
    MediaMetadata? Metadata,
    OnlineMediaProbeError? Error)
{
    public static OnlineMediaProbeResult Success(
        string? title,
        TimeSpan? duration,
        string? thumbnailUrl,
        string? extractor,
        bool isPlaylist,
        MediaMetadata? metadata = null) =>
        new(true, title, duration, thumbnailUrl, extractor, isPlaylist, metadata, null);

    public static OnlineMediaProbeResult Failure(OnlineMediaProbeError error) =>
        new(false, null, null, null, null, false, null, error);
}
