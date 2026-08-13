using VideoToMp3.Core.Models;
using VideoToMp3.Core.Services;

namespace VideoToMp3.Infrastructure.Queue;

public sealed class ConversionQueueService(
    IMediaProbeService mediaProbeService,
    IFFmpegService ffmpegService) : IConversionQueueService
{
    private readonly object _syncRoot = new();
    private readonly Queue<ConversionJob> _pendingJobs = new();
    private readonly HashSet<Guid> _pendingJobIds = [];
    private bool _isRunning;

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

    public event EventHandler? StateChanged;

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

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            while (TryDequeue(out var job))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await ProcessJobAsync(job, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    Fail(job, $"Lỗi không mong đợi khi xử lý job: {exception.Message}");
                }
            }
        }
        finally
        {
            lock (_syncRoot)
            {
                _isRunning = false;
            }

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

        if (job.SourceType != ConversionSourceType.LocalFile || job.InputFilePath is null)
        {
            Fail(job, "Chuyển đổi URL sẽ được hỗ trợ ở bước sau.");
            return;
        }

        var probeResult = await mediaProbeService.ProbeAsync(
            job.InputFilePath,
            cancellationToken);
        if (!probeResult.IsSuccess)
        {
            Fail(job, probeResult.Error?.Message ?? "Không thể phân tích file video.");
            return;
        }

        if (!probeResult.HasAudioStream)
        {
            Fail(job, "File video không có audio stream.");
            return;
        }

        job.Duration = probeResult.Duration;
        job.Status = ConversionJobStatus.Converting;
        job.CurrentStage = "Đang chuyển đổi";
        var progress = new SynchronousProgress<double>(value => job.Progress = value);
        var conversionResult = await ffmpegService.ConvertLocalToMp3Async(
            job,
            progress,
            cancellationToken);

        if (!conversionResult.IsSuccess)
        {
            Fail(job, conversionResult.ErrorMessage ?? "Không thể chuyển đổi file video.");
            return;
        }

        job.Progress = 100;
        job.Status = ConversionJobStatus.Completed;
        job.CurrentStage = "Hoàn thành";
        job.CompletedAt = DateTimeOffset.UtcNow;
    }

    private static void Fail(ConversionJob job, string message)
    {
        job.Status = ConversionJobStatus.Failed;
        job.CurrentStage = "Thất bại";
        job.ErrorMessage = message;
        job.CompletedAt = DateTimeOffset.UtcNow;
    }

    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
