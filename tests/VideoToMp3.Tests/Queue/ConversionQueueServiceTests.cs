using VideoToMp3.Core.Media;
using VideoToMp3.Core.Models;
using VideoToMp3.Core.Online;
using VideoToMp3.Core.Services;
using VideoToMp3.Infrastructure.Queue;

namespace VideoToMp3.Tests.Queue;

public sealed class ConversionQueueServiceTests
{
    [Fact]
    public async Task StartAsync_ProcessesWaitingJobsSequentiallyAndSkipsCompletedJobs()
    {
        var ffmpeg = new TrackingFFmpegService();
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg);
        var completed = CreateJob("completed.mp4");
        completed.Status = ConversionJobStatus.Completed;
        var first = CreateJob("first.mp4");
        var second = CreateJob("second.mp4");
        queue.Enqueue(completed);
        queue.Enqueue(first);
        queue.Enqueue(second);

        await queue.StartAsync();

        Assert.Equal(1, ffmpeg.MaximumConcurrency);
        Assert.Equal([first.Id, second.Id], ffmpeg.ProcessedJobIds);
        Assert.Equal(ConversionJobStatus.Completed, first.Status);
        Assert.Equal(ConversionJobStatus.Completed, second.Status);
        Assert.Equal(100, first.Progress);
        Assert.False(queue.IsRunning);
    }

    [Fact]
    public async Task Enqueue_WhileRunning_AppendsJobToCurrentRun()
    {
        var ffmpeg = new TrackingFFmpegService(blockFirstJob: true);
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg);
        var first = CreateJob("first.mp4");
        var addedWhileRunning = CreateJob("added.mp4");
        queue.Enqueue(first);

        var runTask = queue.StartAsync();
        await ffmpeg.FirstJobStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(queue.IsRunning);
        queue.Enqueue(addedWhileRunning);
        ffmpeg.ReleaseFirstJob();
        await runTask;

        Assert.Equal([first.Id, addedWhileRunning.Id], ffmpeg.ProcessedJobIds);
        Assert.Equal(ConversionJobStatus.Completed, addedWhileRunning.Status);
    }

    [Fact]
    public async Task StartAsync_DoesNotAutomaticallyRetryFailedJob()
    {
        var ffmpeg = new TrackingFFmpegService(fail: true);
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg);
        var job = CreateJob("failure.mp4");
        queue.Enqueue(job);

        await queue.StartAsync();
        queue.Enqueue(job);
        await queue.StartAsync();

        Assert.Equal(ConversionJobStatus.Failed, job.Status);
        Assert.Single(ffmpeg.ProcessedJobIds);
        Assert.NotEmpty(job.ErrorMessage!);
    }

    [Fact]
    public async Task StartAsync_FailsFileWithoutAudioBeforeConversion()
    {
        var ffmpeg = new TrackingFFmpegService();
        var queue = new ConversionQueueService(
            new StubProbeService(MediaProbeResult.Success(
                TimeSpan.FromSeconds(5),
                hasAudioStream: false,
                "mp4",
                null)),
            ffmpeg);
        var job = CreateJob("silent.mp4");
        queue.Enqueue(job);

        await queue.StartAsync();

        Assert.Equal(ConversionJobStatus.Failed, job.Status);
        Assert.Contains("audio stream", job.ErrorMessage);
        Assert.Empty(ffmpeg.ProcessedJobIds);
    }

    [Fact]
    public async Task Cancel_ActiveJob_ContinuesWithNextWaitingJob()
    {
        var ffmpeg = new TrackingFFmpegService(blockFirstJob: true);
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg);
        var first = CreateJob("cancel-active.mp4");
        var second = CreateJob("continue.mp4");
        queue.Enqueue(first);
        queue.Enqueue(second);

        var runTask = queue.StartAsync();
        await ffmpeg.FirstJobStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        queue.Cancel(first);
        await runTask;

        Assert.Equal(ConversionJobStatus.Canceled, first.Status);
        Assert.Equal(ConversionJobStatus.Completed, second.Status);
        Assert.Equal([first.Id, second.Id], ffmpeg.ProcessedJobIds);
    }

    [Fact]
    public async Task Cancel_PendingJob_PreventsItFromStarting()
    {
        var ffmpeg = new TrackingFFmpegService(blockFirstJob: true);
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg);
        var first = CreateJob("active.mp4");
        var pending = CreateJob("cancel-pending.mp4");
        queue.Enqueue(first);
        queue.Enqueue(pending);

        var runTask = queue.StartAsync();
        await ffmpeg.FirstJobStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        queue.Cancel(pending);
        ffmpeg.ReleaseFirstJob();
        await runTask;

        Assert.Equal(ConversionJobStatus.Canceled, pending.Status);
        Assert.DoesNotContain(pending.Id, ffmpeg.ProcessedJobIds);
    }

    [Fact]
    public async Task CancelAll_CancelsActiveAndPendingJobs()
    {
        var ffmpeg = new TrackingFFmpegService(blockFirstJob: true);
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg);
        var active = CreateJob("active-all.mp4");
        var pending = CreateJob("pending-all.mp4");
        queue.Enqueue(active);
        queue.Enqueue(pending);

        var runTask = queue.StartAsync();
        await ffmpeg.FirstJobStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        queue.CancelAll();
        await runTask;

        Assert.Equal(ConversionJobStatus.Canceled, active.Status);
        Assert.Equal(ConversionJobStatus.Canceled, pending.Status);
        Assert.False(queue.IsRunning);
    }

    [Fact]
    public async Task StartAsync_MarksUnsupportedUrlFailedWithClearMessage()
    {
        var onlineResult = OnlineMediaProbeResult.Failure(new OnlineMediaProbeError(
            OnlineMediaProbeErrorCode.UnsupportedUrl,
            "Nguồn URL này không được yt-dlp hỗ trợ."));
        var queue = new ConversionQueueService(
            new SuccessfulProbeService(),
            new TrackingFFmpegService(),
            new StubYtDlpService(onlineResult));
        var job = new ConversionJob(
            ConversionSourceType.Url,
            "https://unsupported.example/video",
            Path.GetTempPath());
        queue.Enqueue(job);

        await queue.StartAsync();

        Assert.Equal(ConversionJobStatus.Failed, job.Status);
        Assert.Contains("không được", job.ErrorMessage);
    }

    [Fact]
    public async Task StartAsync_DownloadsConvertsAndCleansTemporaryUrlMedia()
    {
        var probe = OnlineMediaProbeResult.Success(
            "Online sample",
            TimeSpan.FromSeconds(10),
            null,
            "Example",
            false);
        var ytDlp = new StubYtDlpService(probe, downloadSucceeds: true);
        var ffmpeg = new TrackingFFmpegService();
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg, ytDlp);
        var job = new ConversionJob(
            ConversionSourceType.Url,
            "https://example.com/video",
            Path.GetTempPath());
        queue.Enqueue(job);

        await queue.StartAsync();

        Assert.Equal(ConversionJobStatus.Completed, job.Status);
        Assert.Equal("Online sample", job.DisplayName);
        Assert.Equal(100, job.Progress);
        Assert.NotNull(ytDlp.DownloadDirectory);
        Assert.False(Directory.Exists(ytDlp.DownloadDirectory));
        Assert.Equal(job.Id, Assert.Single(ffmpeg.ProcessedJobIds));
    }

    [Fact]
    public async Task Cancel_OnlineDownload_CleansTemporaryDirectory()
    {
        var ytDlp = new BlockingYtDlpService();
        var queue = new ConversionQueueService(
            new SuccessfulProbeService(),
            new TrackingFFmpegService(),
            ytDlp);
        var job = new ConversionJob(
            ConversionSourceType.Url,
            "https://example.com/video",
            Path.GetTempPath());
        queue.Enqueue(job);

        var runTask = queue.StartAsync();
        await ytDlp.DownloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        queue.Cancel(job);
        await runTask;

        Assert.Equal(ConversionJobStatus.Canceled, job.Status);
        Assert.NotNull(ytDlp.DownloadDirectory);
        Assert.False(Directory.Exists(ytDlp.DownloadDirectory));
    }

    private static ConversionJob CreateJob(string fileName) =>
        new(
            ConversionSourceType.LocalFile,
            Path.Combine(Path.GetTempPath(), fileName),
            Path.GetTempPath());

    private sealed class SuccessfulProbeService : IMediaProbeService
    {
        public Task<MediaProbeResult> ProbeAsync(
            string filePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(MediaProbeResult.Success(
                TimeSpan.FromSeconds(10),
                hasAudioStream: true,
                "mp4",
                null));
    }

    private sealed class StubProbeService(MediaProbeResult result) : IMediaProbeService
    {
        public Task<MediaProbeResult> ProbeAsync(
            string filePath,
            CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed class TrackingFFmpegService(
        bool blockFirstJob = false,
        bool fail = false) : IFFmpegService
    {
        private readonly TaskCompletionSource _releaseFirstJob =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeCount;

        public TaskCompletionSource FirstJobStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<Guid> ProcessedJobIds { get; } = [];
        public int MaximumConcurrency { get; private set; }

        public async Task<AudioConversionResult> ConvertLocalToMp3Async(
            ConversionJob job,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var activeCount = Interlocked.Increment(ref _activeCount);
            MaximumConcurrency = Math.Max(MaximumConcurrency, activeCount);
            ProcessedJobIds.Add(job.Id);
            try
            {
                if (blockFirstJob && ProcessedJobIds.Count == 1)
                {
                    FirstJobStarted.TrySetResult();
                    await _releaseFirstJob.Task.WaitAsync(cancellationToken);
                }

                progress?.Report(50);
                return fail
                    ? AudioConversionResult.Failure("Conversion failed")
                    : AudioConversionResult.Success(Path.ChangeExtension(job.Source, ".mp3"));
            }
            finally
            {
                Interlocked.Decrement(ref _activeCount);
            }
        }

        public Task<AudioConversionResult> ConvertDownloadedToMp3Async(
            ConversionJob job,
            string downloadedFilePath,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default) =>
            ConvertLocalToMp3Async(job, progress, cancellationToken);

        public void ReleaseFirstJob() => _releaseFirstJob.TrySetResult();
    }

    private sealed class StubYtDlpService(
        OnlineMediaProbeResult result,
        bool downloadSucceeds = false) : IYtDlpService
    {
        public string? DownloadDirectory { get; private set; }

        public Task<OnlineMediaProbeResult> ProbeAsync(
            string url,
            CancellationToken cancellationToken = default) => Task.FromResult(result);

        public Task<OnlineMediaDownloadResult> DownloadAsync(
            string url,
            string temporaryDirectory,
            CancellationToken cancellationToken = default)
        {
            DownloadDirectory = temporaryDirectory;
            if (!downloadSucceeds)
            {
                return Task.FromResult(OnlineMediaDownloadResult.Failure(
                    new OnlineMediaProbeError(
                        OnlineMediaProbeErrorCode.ProbeFailed,
                        "Download failed")));
            }

            Directory.CreateDirectory(temporaryDirectory);
            var filePath = Path.Combine(temporaryDirectory, "source.webm");
            File.WriteAllText(filePath, "audio");
            return Task.FromResult(OnlineMediaDownloadResult.Success(filePath));
        }
    }

    private sealed class BlockingYtDlpService : IYtDlpService
    {
        public TaskCompletionSource DownloadStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? DownloadDirectory { get; private set; }

        public Task<OnlineMediaProbeResult> ProbeAsync(
            string url,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(OnlineMediaProbeResult.Success(
                "Online sample", TimeSpan.FromSeconds(10), null, "Example", false));

        public async Task<OnlineMediaDownloadResult> DownloadAsync(
            string url,
            string temporaryDirectory,
            CancellationToken cancellationToken = default)
        {
            DownloadDirectory = temporaryDirectory;
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(Path.Combine(temporaryDirectory, "source.part"), "partial");
            DownloadStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable");
        }
    }
}
