namespace VideoToMp3.Core.Online;

public sealed record OnlineMediaDownloadResult(
    bool IsSuccess,
    string? DownloadedFilePath,
    OnlineMediaProbeError? Error)
{
    public static OnlineMediaDownloadResult Success(string downloadedFilePath) =>
        new(true, downloadedFilePath, null);

    public static OnlineMediaDownloadResult Failure(OnlineMediaProbeError error) =>
        new(false, null, error);
}
