using VideoToMp3.Core.Media;
using VideoToMp3.Core.Models;
using VideoToMp3.Core.Online;
using VideoToMp3.Core.Services;
using VideoToMp3.Infrastructure.Queue;

namespace VideoToMp3.Tests.Queue;

public sealed class ConversionQueueServiceTests
{
    [Fact]
    public async Task StartAsync_ProcessesJobsInParallelWithoutExceedingConfiguredLimit()
    {
        var ffmpeg = new GatedFFmpegService(expectedStarts: 2);
        var queue = new ConversionQueueService(
            new SuccessfulProbeService(),
            ffmpeg,
            concurrency: 2);
        var jobs = Enumerable.Range(1, 4).Select(index => CreateJob($"parallel-{index}.mp4")).ToArray();
        foreach (var job in jobs) queue.Enqueue(job);

        var runTask = queue.StartAsync();
        await ffmpeg.ExpectedStartsReached.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, ffmpeg.MaximumConcurrency);
        ffmpeg.Release();
        await runTask;

        Assert.All(jobs, job => Assert.Equal(ConversionJobStatus.Completed, job.Status));
        Assert.Equal(2, ffmpeg.MaximumConcurrency);
    }

    [Fact]
    public async Task Cancel_OneParallelJob_DoesNotCancelOtherActiveJob()
    {
        var ffmpeg = new GatedFFmpegService(expectedStarts: 2);
        var queue = new ConversionQueueService(
            new SuccessfulProbeService(),
            ffmpeg,
            concurrency: 2);
        var canceled = CreateJob("cancel-one.mp4");
        var completed = CreateJob("complete-other.mp4");
        queue.Enqueue(canceled);
        queue.Enqueue(completed);

        var runTask = queue.StartAsync();
        await ffmpeg.ExpectedStartsReached.Task.WaitAsync(TimeSpan.FromSeconds(5));
        queue.Cancel(canceled);
        ffmpeg.Release();
        await runTask;

        Assert.Equal(ConversionJobStatus.Canceled, canceled.Status);
        Assert.Equal(ConversionJobStatus.Completed, completed.Status);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 3)]
    [InlineData(99, 4)]
    public void Concurrency_IsClampedToSafeRange(int requested, int expected)
    {
        var queue = new ConversionQueueService(
            new SuccessfulProbeService(),
            new TrackingFFmpegService(),
            concurrency: requested);

        Assert.Equal(expected, queue.Concurrency);
        queue.Concurrency = requested;
        Assert.Equal(expected, queue.Concurrency);
    }

    [Fact]
    public async Task StartAsync_ProcessesWaitingJobsSequentiallyAndSkipsCompletedJobs()
    {
        var ffmpeg = new TrackingFFmpegService();
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg, concurrency: 1);
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
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg, concurrency: 1);
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
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg, concurrency: 1);
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
    public async Task StartAsync_LogsTechnicalDetailButShowsUserMessageOnJob()
    {
        var logger = new RecordingLogger();
        var ffmpeg = new TechnicalFailureFFmpegService();
        var queue = new ConversionQueueService(
            new SuccessfulProbeService(),
            ffmpeg,
            appLogger: logger);
        var job = CreateJob("failure-details.mp4");
        queue.Enqueue(job);

        await queue.StartAsync();

        Assert.Equal("Không thể mã hóa MP3.", job.ErrorMessage);
        Assert.DoesNotContain("encoder internals", job.ErrorMessage);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(job.Id, entry.JobId);
        Assert.Equal("encoder internals", entry.TechnicalDetails);
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
    public async Task StartAsync_CorruptLocalFileFailsBeforeConversion()
    {
        var ffmpeg = new TrackingFFmpegService();
        var queue = new ConversionQueueService(
            new StubProbeService(MediaProbeResult.Failure(new MediaProbeError(
                MediaProbeErrorCode.InvalidOutput,
                "File media bị hỏng hoặc không đọc được.",
                "invalid data"))),
            ffmpeg);
        var job = CreateJob("corrupt.mp4");
        queue.Enqueue(job);

        await queue.StartAsync();

        Assert.Equal(ConversionJobStatus.Failed, job.Status);
        Assert.Contains("hỏng", job.ErrorMessage);
        Assert.Empty(ffmpeg.ProcessedJobIds);
    }

    [Fact]
    public async Task Cancel_ActiveJob_ContinuesWithNextWaitingJob()
    {
        var ffmpeg = new TrackingFFmpegService(blockFirstJob: true);
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg, concurrency: 1);
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
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg, concurrency: 1);
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
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg, concurrency: 1);
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
        var reportedProgress = new List<double>();
        job.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ConversionJob.Progress))
            {
                reportedProgress.Add(job.Progress);
            }
        };
        queue.Enqueue(job);

        await queue.StartAsync();

        Assert.Equal(ConversionJobStatus.Completed, job.Status);
        Assert.Equal("Online sample", job.DisplayName);
        Assert.Equal(100, job.Progress);
        Assert.NotNull(ytDlp.DownloadDirectory);
        Assert.False(Directory.Exists(ytDlp.DownloadDirectory));
        Assert.Equal(job.Id, Assert.Single(ffmpeg.ProcessedJobIds));
        Assert.Contains(5, reportedProgress);
        Assert.Contains(70, reportedProgress);
        Assert.Contains(84.5, reportedProgress);
        Assert.Equal(100, reportedProgress[^1]);
    }

    [Fact]
    public async Task StartAsync_ThumbnailFailureLogsWarningAndStillCompletes()
    {
        var probe = OnlineMediaProbeResult.Success(
            "Online sample",
            TimeSpan.FromSeconds(10),
            "https://cdn.example.com/cover.jpg",
            "Example",
            false);
        var logger = new RecordingLogger();
        var queue = new ConversionQueueService(
            new SuccessfulProbeService(),
            new TrackingFFmpegService(),
            new StubYtDlpService(probe, downloadSucceeds: true),
            logger);
        var job = new ConversionJob(
            ConversionSourceType.Url,
            "https://example.com/video",
            Path.GetTempPath());
        queue.Enqueue(job);

        await queue.StartAsync();

        Assert.Equal(ConversionJobStatus.Completed, job.Status);
        Assert.Single(logger.Warnings);
        Assert.Contains("thumbnail", logger.Warnings[0].UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartAsync_DownloadsEmbedsAndCleansOnlineThumbnail()
    {
        var probe = OnlineMediaProbeResult.Success(
            "Online sample",
            TimeSpan.FromSeconds(10),
            "https://cdn.example.com/cover.jpg",
            "Example",
            false);
        var ytDlp = new StubYtDlpService(probe, downloadSucceeds: true, thumbnailSucceeds: true);
        var ffmpeg = new TrackingFFmpegService();
        var queue = new ConversionQueueService(new SuccessfulProbeService(), ffmpeg, ytDlp);
        var job = new ConversionJob(
            ConversionSourceType.Url,
            "https://example.com/video",
            Path.GetTempPath());
        queue.Enqueue(job);

        await queue.StartAsync();

        Assert.Equal(ConversionJobStatus.Completed, job.Status);
        Assert.NotNull(Assert.Single(ffmpeg.ThumbnailPaths));
        Assert.Null(job.ThumbnailLocalPath);
        Assert.False(Directory.Exists(ytDlp.DownloadDirectory));
    }

    [Fact]
    public async Task StartAsync_ExpandsPlaylistIntoBoundedIndependentJobs()
    {
        var probe = OnlineMediaProbeResult.Success(
            "Playlist",
            null,
            null,
            "Example",
            true);
        var entries = new[]
        {
            new OnlinePlaylistEntry("https://example.com/1", "One", TimeSpan.FromSeconds(10), null, null),
            new OnlinePlaylistEntry("https://example.com/2", "Two", TimeSpan.FromSeconds(20), null, null)
        };
        var ytDlp = new StubYtDlpService(
            probe,
            downloadSucceeds: true,
            playlistEntries: entries);
        var queue = new ConversionQueueService(
            new SuccessfulProbeService(),
            new TrackingFFmpegService(),
            ytDlp);
        var playlist = new ConversionJob(
            ConversionSourceType.Url,
            "https://example.com/playlist",
            Path.GetTempPath());
        PlaylistExpandedEventArgs? expanded = null;
        queue.PlaylistExpanded += (_, args) => expanded = args;
        queue.Enqueue(playlist);

        await queue.StartAsync();

        Assert.Equal(ConversionJobStatus.Expanded, playlist.Status);
        Assert.NotNull(expanded);
        Assert.Equal(2, expanded.ItemJobs.Count);
        Assert.All(expanded.ItemJobs, item => Assert.True(item.IsPlaylistItem));
        Assert.All(expanded.ItemJobs, item => Assert.Equal(ConversionJobStatus.Completed, item.Status));
        Assert.Equal(100, ytDlp.MaximumPlaylistItems);
        Assert.Equal(2, ytDlp.SingleProbeCount);
    }

    [Fact]
    public async Task Cancel_PlaylistExpansion_MarksContainerCanceled()
    {
        var ytDlp = new BlockingPlaylistYtDlpService();
        var queue = new ConversionQueueService(
            new SuccessfulProbeService(),
            new TrackingFFmpegService(),
            ytDlp);
        var playlist = new ConversionJob(
            ConversionSourceType.Url,
            "https://example.com/playlist",
            Path.GetTempPath());
        queue.Enqueue(playlist);

        var runTask = queue.StartAsync();
        await ytDlp.ExpansionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        queue.Cancel(playlist);
        await runTask;

        Assert.Equal(ConversionJobStatus.Canceled, playlist.Status);
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
        public List<string?> ThumbnailPaths { get; } = [];
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
            CancellationToken cancellationToken = default)
        {
            ThumbnailPaths.Add(job.ThumbnailLocalPath);
            return ConvertLocalToMp3Async(job, progress, cancellationToken);
        }

        public void ReleaseFirstJob() => _releaseFirstJob.TrySetResult();
    }

    private sealed class GatedFFmpegService(int expectedStarts) : IFFmpegService
    {
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeCount;
        private int _startedCount;

        public TaskCompletionSource ExpectedStartsReached { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int MaximumConcurrency { get; private set; }

        public async Task<AudioConversionResult> ConvertLocalToMp3Async(
            ConversionJob job,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var active = Interlocked.Increment(ref _activeCount);
            MaximumConcurrency = Math.Max(MaximumConcurrency, active);
            if (Interlocked.Increment(ref _startedCount) >= expectedStarts)
            {
                ExpectedStartsReached.TrySetResult();
            }

            try
            {
                await _release.Task.WaitAsync(cancellationToken);
                return AudioConversionResult.Success(Path.ChangeExtension(job.Source, ".mp3"));
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

        public void Release() => _release.TrySetResult();
    }

    private sealed class TechnicalFailureFFmpegService : IFFmpegService
    {
        public Task<AudioConversionResult> ConvertLocalToMp3Async(
            ConversionJob job,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(AudioConversionResult.Failure(
                "Không thể mã hóa MP3.",
                "encoder internals"));

        public Task<AudioConversionResult> ConvertDownloadedToMp3Async(
            ConversionJob job,
            string downloadedFilePath,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default) =>
            ConvertLocalToMp3Async(job, progress, cancellationToken);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<(Guid JobId, string UserMessage, string? TechnicalDetails)> Entries { get; } = [];
        public List<(Guid JobId, string UserMessage, string? TechnicalDetails)> Warnings { get; } = [];

        public void LogError(Guid jobId, string userMessage, string? technicalDetails = null) =>
            Entries.Add((jobId, userMessage, technicalDetails));

        public void LogWarning(Guid jobId, string userMessage, string? technicalDetails = null) =>
            Warnings.Add((jobId, userMessage, technicalDetails));
    }

    private sealed class StubYtDlpService(
        OnlineMediaProbeResult result,
        bool downloadSucceeds = false,
        bool thumbnailSucceeds = false,
        IReadOnlyList<OnlinePlaylistEntry>? playlistEntries = null) : IYtDlpService
    {
        public string? DownloadDirectory { get; private set; }
        public int MaximumPlaylistItems { get; private set; }
        public int SingleProbeCount { get; private set; }

        public Task<OnlineMediaProbeResult> ProbeAsync(
            string url,
            CancellationToken cancellationToken = default) => Task.FromResult(result);

        public Task<OnlineMediaProbeResult> ProbeSingleAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            SingleProbeCount++;
            return Task.FromResult(OnlineMediaProbeResult.Success(
                Path.GetFileName(new Uri(url).AbsolutePath),
                TimeSpan.FromSeconds(10),
                null,
                "Example",
                false));
        }

        public Task<OnlinePlaylistExpansionResult> ExpandPlaylistAsync(
            string url,
            int maximumItems,
            CancellationToken cancellationToken = default)
        {
            MaximumPlaylistItems = maximumItems;
            return Task.FromResult(OnlinePlaylistExpansionResult.Success(
                "Playlist",
                playlistEntries ?? [],
                false));
        }

        public Task<OnlineMediaDownloadResult> DownloadAsync(
            string url,
            string temporaryDirectory,
            IProgress<double>? progress = null,
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
            progress?.Report(100);
            return Task.FromResult(OnlineMediaDownloadResult.Success(filePath));
        }

        public Task<OnlineThumbnailDownloadResult> DownloadThumbnailAsync(
            string url,
            string temporaryDirectory,
            CancellationToken cancellationToken = default)
        {
            if (!thumbnailSucceeds)
            {
                return Task.FromResult(OnlineThumbnailDownloadResult.Failure(
                    new OnlineMediaProbeError(OnlineMediaProbeErrorCode.ProbeFailed, "Thumbnail failed")));
            }

            var path = Path.Combine(temporaryDirectory, "cover.jpg");
            File.WriteAllBytes(path, [1]);
            return Task.FromResult(OnlineThumbnailDownloadResult.Success(path));
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
            IProgress<double>? progress = null,
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

    private sealed class BlockingPlaylistYtDlpService : IYtDlpService
    {
        public TaskCompletionSource ExpansionStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<OnlineMediaProbeResult> ProbeAsync(
            string url,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(OnlineMediaProbeResult.Success(
                "Playlist", null, null, "Example", true));

        public async Task<OnlinePlaylistExpansionResult> ExpandPlaylistAsync(
            string url,
            int maximumItems,
            CancellationToken cancellationToken = default)
        {
            ExpansionStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable");
        }

        public Task<OnlineMediaDownloadResult> DownloadAsync(
            string url,
            string temporaryDirectory,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Playlist container must not download media.");
    }
}
