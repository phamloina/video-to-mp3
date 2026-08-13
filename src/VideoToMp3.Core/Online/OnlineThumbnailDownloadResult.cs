namespace VideoToMp3.Core.Online;

public sealed record OnlineThumbnailDownloadResult(
    bool IsSuccess,
    string? ThumbnailFilePath,
    OnlineMediaProbeError? Error)
{
    public static OnlineThumbnailDownloadResult Success(string thumbnailFilePath) =>
        new(true, thumbnailFilePath, null);

    public static OnlineThumbnailDownloadResult Failure(OnlineMediaProbeError error) =>
        new(false, null, error);
}
