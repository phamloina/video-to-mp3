namespace VideoToMp3.Core.Online;

using VideoToMp3.Core.Models;

public sealed class PlaylistExpandedEventArgs(
    ConversionJob playlistJob,
    IReadOnlyList<ConversionJob> itemJobs,
    bool wasLimited) : EventArgs
{
    public ConversionJob PlaylistJob { get; } = playlistJob;
    public IReadOnlyList<ConversionJob> ItemJobs { get; } = itemJobs;
    public bool WasLimited { get; } = wasLimited;
}
