namespace VideoToMp3.Core.Online;

using VideoToMp3.Core.Models;

public sealed record OnlinePlaylistEntry(
    string Url,
    string? Title,
    TimeSpan? Duration,
    string? ThumbnailUrl,
    MediaMetadata? Metadata);
