using VideoToMp3.Core.Dependencies;
using VideoToMp3.Core.Media;
using VideoToMp3.Core.Models;
using VideoToMp3.Core.Services;
using VideoToMp3.Infrastructure.Processes;

namespace VideoToMp3.Infrastructure.Media;

public sealed class FFmpegService : IFFmpegService
{
    private readonly IMediaToolResolver _toolResolver;
    private readonly IOutputPathResolver _outputPathResolver;
    private readonly IProcessRunner _processRunner;

    public FFmpegService(
        IMediaToolResolver toolResolver,
        IOutputPathResolver? outputPathResolver = null,
        IProcessRunner? processRunner = null)
    {
        _toolResolver = toolResolver ?? throw new ArgumentNullException(nameof(toolResolver));
        _outputPathResolver = outputPathResolver ?? new OutputPathResolver();
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public async Task<AudioConversionResult> ConvertLocalToMp3Async(
        ConversionJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (job.SourceType != ConversionSourceType.LocalFile ||
            string.IsNullOrWhiteSpace(job.InputFilePath))
        {
            return AudioConversionResult.Failure("Job không phải là file local hợp lệ.");
        }

        if (!File.Exists(job.InputFilePath))
        {
            return AudioConversionResult.Failure("Không tìm thấy file video nguồn.");
        }

        var ffmpeg = _toolResolver.Resolve(MediaTool.Ffmpeg);
        if (!ffmpeg.IsAvailable || string.IsNullOrWhiteSpace(ffmpeg.ExecutablePath))
        {
            return AudioConversionResult.Failure(
                ffmpeg.ErrorMessage ?? "Không tìm thấy FFmpeg.");
        }

        try
        {
            Directory.CreateDirectory(job.OutputDirectory);
            var outputPath = _outputPathResolver.ResolveAvailableMp3Path(
                job.InputFilePath,
                job.OutputDirectory);
            var arguments = new[]
            {
                "-hide_banner",
                "-nostdin",
                "-n",
                "-i", job.InputFilePath,
                "-vn",
                "-codec:a", "libmp3lame",
                "-b:a", $"{job.RequestedBitrate}k",
                outputPath
            };

            var result = await _processRunner
                .RunAsync(ffmpeg.ExecutablePath, arguments, cancellationToken)
                .ConfigureAwait(false);

            if (result.ExitCode != 0 || !File.Exists(outputPath))
            {
                DeletePartialOutput(outputPath);
                return AudioConversionResult.Failure(
                    $"FFmpeg không thể chuyển đổi file (mã lỗi {result.ExitCode}).",
                    result.StandardError);
            }

            job.OutputFilePath = outputPath;
            return AudioConversionResult.Success(outputPath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return AudioConversionResult.Failure(
                "Không thể chuyển đổi file video sang MP3.",
                exception.Message);
        }
    }

    private static void DeletePartialOutput(string outputPath)
    {
        try
        {
            File.Delete(outputPath);
        }
        catch (IOException)
        {
            // The original FFmpeg error remains the useful failure detail.
        }
        catch (UnauthorizedAccessException)
        {
            // Cleanup is best effort; STEP 13 defines the full partial-file policy.
        }
    }
}
