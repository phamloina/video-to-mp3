using VideoToMp3.Core.Media;
using VideoToMp3.Core.Models;

namespace VideoToMp3.Core.Services;

public interface IFFmpegService
{
    Task<AudioConversionResult> ConvertLocalToMp3Async(
        ConversionJob job,
        CancellationToken cancellationToken = default);
}
