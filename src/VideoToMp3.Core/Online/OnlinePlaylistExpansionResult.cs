namespace VideoToMp3.Core.Online;

public sealed record OnlinePlaylistExpansionResult(
    bool IsSuccess,
    string? Title,
    IReadOnlyList<OnlinePlaylistEntry> Entries,
    bool WasLimited,
    OnlineMediaProbeError? Error)
{
    public static OnlinePlaylistExpansionResult Success(
        string? title,
        IReadOnlyList<OnlinePlaylistEntry> entries,
        bool wasLimited) => new(true, title, entries, wasLimited, null);

    public static OnlinePlaylistExpansionResult Failure(OnlineMediaProbeError error) =>
        new(false, null, [], false, error);
}
