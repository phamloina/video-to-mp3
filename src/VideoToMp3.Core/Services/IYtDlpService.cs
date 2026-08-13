using VideoToMp3.Core.Online;

namespace VideoToMp3.Core.Services;

public interface IYtDlpService
{
    Task<OnlineMediaProbeResult> ProbeAsync(
        string url,
        CancellationToken cancellationToken = default);

    Task<OnlineMediaProbeResult> ProbeSingleAsync(
        string url,
        CancellationToken cancellationToken = default) => ProbeAsync(url, cancellationToken);

    Task<OnlinePlaylistExpansionResult> ExpandPlaylistAsync(
        string url,
        int maximumItems,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(OnlinePlaylistExpansionResult.Failure(new OnlineMediaProbeError(
            OnlineMediaProbeErrorCode.ProbeFailed,
            "Không hỗ trợ mở rộng playlist.")));

    Task<OnlineMediaDownloadResult> DownloadAsync(
        string url,
        string temporaryDirectory,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    Task<OnlineThumbnailDownloadResult> DownloadThumbnailAsync(
        string url,
        string temporaryDirectory,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(OnlineThumbnailDownloadResult.Failure(new OnlineMediaProbeError(
            OnlineMediaProbeErrorCode.ProbeFailed,
            "Không hỗ trợ tải thumbnail.")));
}
