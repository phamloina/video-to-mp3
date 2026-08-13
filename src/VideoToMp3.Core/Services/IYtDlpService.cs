using VideoToMp3.Core.Online;

namespace VideoToMp3.Core.Services;

public interface IYtDlpService
{
    Task<OnlineMediaProbeResult> ProbeAsync(
        string url,
        CancellationToken cancellationToken = default);

    Task<OnlineMediaDownloadResult> DownloadAsync(
        string url,
        string temporaryDirectory,
        CancellationToken cancellationToken = default);
}
