using VideoToMp3.Core.Media;

namespace VideoToMp3.Core.Services;

public interface IMediaProbeService
{
    Task<MediaProbeResult> ProbeAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
