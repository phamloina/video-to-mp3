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
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (job.SourceType != ConversionSourceType.LocalFile ||
            string.IsNullOrWhiteSpace(job.InputFilePath))
        {
            return AudioConversionResult.Failure("Job không phải là file local hợp lệ.");
        }

        return await ConvertFileToMp3Async(job, job.InputFilePath, progress, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<AudioConversionResult> ConvertDownloadedToMp3Async(
        ConversionJob job,
        string downloadedFilePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        return job.SourceType != ConversionSourceType.Url
            ? Task.FromResult(AudioConversionResult.Failure("Job không phải là URL hợp lệ."))
            : ConvertFileToMp3Async(job, downloadedFilePath, progress, cancellationToken);
    }

    private async Task<AudioConversionResult> ConvertFileToMp3Async(
        ConversionJob job,
        string inputFilePath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(inputFilePath))
        {
            return AudioConversionResult.Failure("Không tìm thấy file media nguồn.");
        }

        var ffmpeg = _toolResolver.Resolve(MediaTool.Ffmpeg);
        if (!ffmpeg.IsAvailable || string.IsNullOrWhiteSpace(ffmpeg.ExecutablePath))
        {
            return AudioConversionResult.Failure(
                ffmpeg.ErrorMessage ?? "Không tìm thấy FFmpeg.");
        }

        string? outputPath = null;
        try
        {
            Directory.CreateDirectory(job.OutputDirectory);
            outputPath = _outputPathResolver.ResolveAvailableMp3Path(
                inputFilePath,
                job.OutputDirectory,
                job.SourceType == ConversionSourceType.Url ? job.DisplayName : null);
            var arguments = new List<string>
            {
                "-hide_banner",
                "-nostdin",
                "-n",
                "-progress", "pipe:1",
                "-nostats",
                "-i", inputFilePath,
                "-vn",
                "-codec:a", "libmp3lame",
                "-b:a", $"{job.RequestedBitrate}k"
            };
            AddMetadataArguments(arguments, job.Metadata);
            arguments.Add(outputPath);

            ProcessRunResult result;
            if (progress is not null && job.Duration is { } duration && duration > TimeSpan.Zero)
            {
                var parser = new FFmpegProgressParser(duration, progress);
                result = await _processRunner
                    .RunWithProgressAsync(
                        ffmpeg.ExecutablePath,
                        arguments,
                        new SynchronousProgress<string>(parser.Parse),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                result = await _processRunner
                    .RunAsync(ffmpeg.ExecutablePath, arguments, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (result.ExitCode != 0 || !File.Exists(outputPath))
            {
                DeletePartialOutput(outputPath);
                return AudioConversionResult.Failure(
                    $"FFmpeg không thể chuyển đổi file (mã lỗi {result.ExitCode}).",
                    result.StandardError);
            }

            job.OutputFilePath = outputPath;
            progress?.Report(100);
            return AudioConversionResult.Success(outputPath);
        }
        catch (OperationCanceledException)
        {
            if (outputPath is not null)
            {
                DeletePartialOutput(outputPath);
            }

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
            // Cleanup is best effort because the original operation owns the failure.
        }
    }

    private static void AddMetadataArguments(List<string> arguments, MediaMetadata? metadata)
    {
        if (metadata is null) return;
        AddMetadata(arguments, "title", metadata.Title);
        AddMetadata(arguments, "artist", metadata.Artist);
        AddMetadata(arguments, "album", metadata.Album);
        if (metadata.TrackNumber is > 0)
        {
            AddMetadata(arguments, "track", metadata.TrackNumber.Value.ToString());
        }
    }

    private static void AddMetadata(List<string> arguments, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        arguments.Add("-metadata");
        arguments.Add($"{key}={value.Trim()}");
    }

    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
