using VideoToMp3.Core.Models;
using VideoToMp3.Core.Services;

namespace VideoToMp3.Infrastructure.Queue;

public sealed class ConversionQueueService(
    IMediaProbeService mediaProbeService,
    IFFmpegService ffmpegService,
    IYtDlpService? ytDlpService = null,
    IAppLogger? appLogger = null) : IConversionQueueService
{
    private readonly IAppLogger _appLogger = appLogger ?? NullAppLogger.Instance;
    private readonly object _syncRoot = new();
    private readonly Queue<ConversionJob> _pendingJobs = new();
    private readonly HashSet<Guid> _pendingJobIds = [];
    private bool _isRunning;
    private CancellationTokenSource? _runCancellation;
    private ConversionJob? _activeJob;
    private CancellationTokenSource? _activeJobCancellation;

    public bool IsRunning
    {
        get
        {
            lock (_syncRoot)
            {
                return _isRunning;
            }
        }
    }

    public ConversionJob? ActiveJob
    {
        get
        {
            lock (_syncRoot)
            {
                return _activeJob;
            }
        }
    }

    public event EventHandler? StateChanged;
    public event EventHandler<ConversionJob>? JobFinished;

    public void Enqueue(ConversionJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (job.Status != ConversionJobStatus.Waiting)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (_pendingJobIds.Add(job.Id))
            {
                _pendingJobs.Enqueue(job);
            }
        }
    }

    public void Cancel(ConversionJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        CancellationTokenSource? cancellation = null;
        lock (_syncRoot)
        {
            if (_activeJob?.Id == job.Id)
            {
                cancellation = _activeJobCancellation;
            }
            else if (_pendingJobIds.Contains(job.Id) &&
                     job.Status == ConversionJobStatus.Waiting)
            {
                MarkCanceled(job);
            }
        }

        cancellation?.Cancel();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CancelAll()
    {
        CancellationTokenSource? cancellation;
        lock (_syncRoot)
        {
            foreach (var job in _pendingJobs)
            {
                if (job.Status == ConversionJobStatus.Waiting)
                {
                    MarkCanceled(job);
                }
            }

            _pendingJobs.Clear();
            _pendingJobIds.Clear();
            cancellation = _runCancellation;
        }

        cancellation?.Cancel();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource runCancellation;
        lock (_syncRoot)
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            _runCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            runCancellation = _runCancellation;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            while (TryDequeue(out var job))
            {
                if (runCancellation.IsCancellationRequested)
                {
                    break;
                }

                using var jobCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    runCancellation.Token);
                lock (_syncRoot)
                {
                    _activeJob = job;
                    _activeJobCancellation = jobCancellation;
                }

                try
                {
                    await ProcessJobAsync(job, jobCancellation.Token);
                }
                catch (OperationCanceledException) when (jobCancellation.IsCancellationRequested)
                {
                    MarkCanceled(job);
                    if (runCancellation.IsCancellationRequested)
                    {
                        break;
                    }
                }
                catch (Exception exception)
                {
                    Fail(job, "Đã xảy ra lỗi không mong đợi khi xử lý. Vui lòng thử lại.", exception.ToString());
                }
                finally
                {
                    lock (_syncRoot)
                    {
                        _activeJob = null;
                        _activeJobCancellation = null;
                    }

                    if (job.Status is ConversionJobStatus.Completed or ConversionJobStatus.Failed or ConversionJobStatus.Canceled)
                    {
                        JobFinished?.Invoke(this, job);
                    }
                }
            }
        }
        finally
        {
            lock (_syncRoot)
            {
                _isRunning = false;
                _runCancellation = null;
            }

            runCancellation.Dispose();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool TryDequeue(out ConversionJob job)
    {
        lock (_syncRoot)
        {
            if (_pendingJobs.Count == 0)
            {
                _isRunning = false;
                job = null!;
                return false;
            }

            job = _pendingJobs.Dequeue();
            _pendingJobIds.Remove(job.Id);
            return true;
        }
    }

    private async Task ProcessJobAsync(
        ConversionJob job,
        CancellationToken cancellationToken)
    {
        if (job.Status != ConversionJobStatus.Waiting)
        {
            return;
        }

        job.StartedAt = DateTimeOffset.UtcNow;
        job.ErrorMessage = null;
        job.Status = ConversionJobStatus.Analyzing;
        job.CurrentStage = "Đang phân tích";

        if (job.SourceType == ConversionSourceType.Url)
        {
            await ProbeUrlJobAsync(job, cancellationToken);
            return;
        }

        if (job.InputFilePath is null)
        {
            Fail(job, "File local không hợp lệ.");
            return;
        }

        var probeResult = await mediaProbeService.ProbeAsync(
            job.InputFilePath,
            cancellationToken);
        if (!probeResult.IsSuccess)
        {
            Fail(
                job,
                probeResult.Error?.Message ?? "Không thể phân tích file video.",
                probeResult.Error?.TechnicalDetails);
            return;
        }

        if (!probeResult.HasAudioStream)
        {
            Fail(job, "File video không có audio stream.");
            return;
        }

        job.Duration = probeResult.Duration;
        job.Metadata = probeResult.Metadata;
        job.Status = ConversionJobStatus.Converting;
        job.CurrentStage = "Đang chuyển đổi";
        var progress = new SynchronousProgress<double>(value => job.Progress = value);
        var conversionResult = await ffmpegService.ConvertLocalToMp3Async(
            job,
            progress,
            cancellationToken);

        if (!conversionResult.IsSuccess)
        {
            Fail(
                job,
                conversionResult.ErrorMessage ?? "Không thể chuyển đổi file video.",
                conversionResult.TechnicalDetails);
            return;
        }

        job.Progress = 100;
        job.Status = ConversionJobStatus.Completed;
        job.CurrentStage = "Hoàn thành";
        job.CompletedAt = DateTimeOffset.UtcNow;
    }

    private async Task ProbeUrlJobAsync(
        ConversionJob job,
        CancellationToken cancellationToken)
    {
        if (ytDlpService is null || job.SourceUrl is null)
        {
            Fail(job, "Chưa cấu hình yt-dlp để phân tích URL.");
            return;
        }

        var result = await ytDlpService.ProbeAsync(job.SourceUrl, cancellationToken);
        if (!result.IsSuccess)
        {
            Fail(job, result.Error?.Message ?? "Không thể phân tích URL.", result.Error?.TechnicalDetails);
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.Title))
        {
            job.DisplayName = result.Title;
            job.Metadata = result.Metadata ?? new MediaMetadata(Title: result.Title);
        }

        job.Duration = result.Duration;
        job.ThumbnailUrl = result.ThumbnailUrl;
        job.Progress = 5;
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "VideoToMp3",
            $"{job.Id:N}-{Guid.NewGuid():N}");
        try
        {
            job.Status = ConversionJobStatus.Downloading;
            job.CurrentStage = "Đang tải";
            var downloadResult = await ytDlpService.DownloadAsync(
                job.SourceUrl,
                temporaryDirectory,
                new SynchronousProgress<double>(value => job.Progress = 5 + value * 0.65),
                cancellationToken);
            if (!downloadResult.IsSuccess || downloadResult.DownloadedFilePath is null)
            {
                Fail(
                    job,
                    downloadResult.Error?.Message ?? "Không thể tải media từ URL.",
                    downloadResult.Error?.TechnicalDetails);
                return;
            }

            job.Status = ConversionJobStatus.Converting;
            job.CurrentStage = "Đang chuyển đổi";
            var conversionResult = await ffmpegService.ConvertDownloadedToMp3Async(
                job,
                downloadResult.DownloadedFilePath,
                new SynchronousProgress<double>(value => job.Progress = 70 + value * 0.29),
                cancellationToken: cancellationToken);
            if (!conversionResult.IsSuccess)
            {
                Fail(
                    job,
                    conversionResult.ErrorMessage ?? "Không thể chuyển media tải về sang MP3.",
                    conversionResult.TechnicalDetails);
                return;
            }

            job.Progress = 100;
            job.Status = ConversionJobStatus.Completed;
            job.CurrentStage = "Hoàn thành";
            job.CompletedAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    private static void DeleteTemporaryDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void Fail(ConversionJob job, string message, string? technicalDetails = null)
    {
        job.Status = ConversionJobStatus.Failed;
        job.CurrentStage = "Thất bại";
        job.ErrorMessage = message;
        job.CompletedAt = DateTimeOffset.UtcNow;
        _appLogger.LogError(job.Id, message, technicalDetails);
    }

    private static void MarkCanceled(ConversionJob job)
    {
        job.Status = ConversionJobStatus.Canceled;
        job.CurrentStage = "Đã hủy";
        job.ErrorMessage = null;
        job.CompletedAt = DateTimeOffset.UtcNow;
    }

    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }

    private sealed class NullAppLogger : IAppLogger
    {
        public static NullAppLogger Instance { get; } = new();
        public void LogError(Guid jobId, string userMessage, string? technicalDetails = null) { }
    }
}
