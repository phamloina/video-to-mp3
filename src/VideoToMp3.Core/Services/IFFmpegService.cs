using VideoToMp3.Core.Media;
using VideoToMp3.Core.Models;

namespace VideoToMp3.Core.Services;

public interface IFFmpegService
{
    Task<AudioConversionResult> ConvertLocalToMp3Async(
        ConversionJob job,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    Task<AudioConversionResult> ConvertDownloadedToMp3Async(
        ConversionJob job,
        string downloadedFilePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
