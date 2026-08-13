namespace VideoToMp3.Core.Models;

public sealed record MediaMetadata(
    string? Title = null,
    string? Artist = null,
    string? Album = null,
    int? TrackNumber = null);
