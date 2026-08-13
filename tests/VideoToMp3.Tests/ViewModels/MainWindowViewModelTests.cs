using VideoToMp3.App.Services;
using VideoToMp3.App.ViewModels;
using VideoToMp3.Core.Dependencies;
using VideoToMp3.Core.History;
using VideoToMp3.Core.Models;
using VideoToMp3.Core.Online;
using VideoToMp3.Core.Services;
using VideoToMp3.Core.Settings;

namespace VideoToMp3.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void ChooseFilesCommand_AddsAllSelectedFiles()
    {
        var picker = new StubFilePickerService(
            @"C:\Media\one.mp4",
            @"C:\Media\two.mkv");
        var viewModel = CreateViewModel(picker);

        viewModel.ChooseFilesCommand.Execute(null);

        Assert.Collection(
            viewModel.Jobs,
            job => Assert.Equal("one.mp4", job.DisplayName),
            job => Assert.Equal("two.mkv", job.DisplayName));
        Assert.All(viewModel.Jobs, job =>
            Assert.Equal(ConversionSourceType.LocalFile, job.SourceType));
        Assert.False(viewModel.IsQueueEmpty);
    }

    [Fact]
    public void AddLocalFiles_SkipsDuplicatesAlreadyInQueue()
    {
        var viewModel = CreateViewModel();

        var firstAddedCount = viewModel.AddLocalFiles([@"C:\Media\clip.mp4"]);
        var secondAddedCount = viewModel.AddLocalFiles([@"c:\media\CLIP.mp4"]);

        Assert.Equal(1, firstAddedCount);
        Assert.Equal(0, secondAddedCount);
        Assert.Single(viewModel.Jobs);
        Assert.Contains("1 nguồn trùng lặp", viewModel.InputValidationMessage);
    }

    [Fact]
    public void AddInputsFromText_AddsMixedSourcesAndReportsInvalidLines()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText =
            "https://example.com/video\nC:\\Media\\local.mp4\ninvalid input";

        var addedCount = viewModel.AddInputsFromText();

        Assert.Equal(2, addedCount);
        Assert.Equal(2, viewModel.Jobs.Count);
        Assert.Contains("invalid input", viewModel.InputValidationMessage);
        Assert.Equal("0 / 2 hoàn thành · 0 lỗi · 0 đã hủy", viewModel.JobSummary);
    }

    [Fact]
    public void Defaults_UseExpectedBitrateAndMusicOutputDirectory()
    {
        var viewModel = CreateViewModel();

        Assert.Equal("320 kbps", viewModel.SelectedBitrate);
        Assert.Equal(
            ["320 kbps", "256 kbps", "192 kbps", "128 kbps"],
            viewModel.BitrateOptions);
        Assert.EndsWith("Video To MP3", viewModel.OutputDirectory);
    }

    [Fact]
    public void Settings_AreRestoredAndSavedWhenChanged()
    {
        var settings = new StubSettingsService(
            new AppSettings(@"D:\Saved", 192, 3, "Dark", false, false));
        var viewModel = CreateViewModel(settingsService: settings);

        Assert.Equal(@"D:\Saved", viewModel.OutputDirectory);
        Assert.Equal("192 kbps", viewModel.SelectedBitrate);
        Assert.Equal(3, viewModel.Concurrency);
        Assert.Equal("Dark", viewModel.SelectedTheme);
        Assert.False(viewModel.NotificationsEnabled);
        Assert.False(viewModel.EmbedThumbnail);

        viewModel.SelectedBitrate = "256 kbps";
        viewModel.Concurrency = 99;
        viewModel.SelectedTheme = "Light";
        viewModel.NotificationsEnabled = true;
        viewModel.EmbedThumbnail = true;

        Assert.Equal(5, settings.SaveCount);
        Assert.Equal(256, settings.LastSaved!.Bitrate);
        Assert.Equal(4, settings.LastSaved.Concurrency);
        Assert.Equal("Light", settings.LastSaved.Theme);
        Assert.True(settings.LastSaved.NotificationsEnabled);
        Assert.True(settings.LastSaved.EmbedThumbnail);
    }

    [Fact]
    public void ConcurrencySetting_IsAppliedToQueueAndClampedToFour()
    {
        var queue = new StubConversionQueueService();
        var settings = new StubSettingsService(new AppSettings(Concurrency: 3));
        var viewModel = CreateViewModel(
            conversionQueueService: queue,
            settingsService: settings);

        Assert.Equal(3, queue.Concurrency);
        viewModel.Concurrency = 9;

        Assert.Equal(4, viewModel.Concurrency);
        Assert.Equal(4, queue.Concurrency);
        Assert.Equal(4, settings.LastSaved?.Concurrency);
    }

    [Fact]
    public void PlaylistExpansion_ReplacesContainerAndReportsItemCount()
    {
        var queue = new StubConversionQueueService();
        var viewModel = CreateViewModel(conversionQueueService: queue);
        viewModel.InputText = "https://example.com/playlist";
        viewModel.AddInputsCommand.Execute(null);
        var playlist = Assert.Single(viewModel.Jobs);
        var items = new[]
        {
            new ConversionJob(ConversionSourceType.Url, "https://example.com/1", @"C:\Output"),
            new ConversionJob(ConversionSourceType.Url, "https://example.com/2", @"C:\Output")
        };

        queue.ExpandPlaylist(playlist, items, wasLimited: false);

        Assert.Equal(items, viewModel.Jobs);
        Assert.Contains("2 mục", viewModel.InputValidationMessage);
    }

    [Fact]
    public async Task History_LoadSearchReAddAndClear_WorkWithoutBlockingQueue()
    {
        var entry = new HistoryEntry(
            Guid.NewGuid(), ConversionSourceType.Url, "https://example.com/video", "Bài hát",
            @"C:\Output", null, 192, ConversionJobStatus.Failed, "failed", DateTimeOffset.UtcNow);
        var history = new StubHistoryService(entry);
        var queue = new StubConversionQueueService();
        var viewModel = CreateViewModel(conversionQueueService: queue, historyService: history);

        await viewModel.LoadHistoryAsync();
        viewModel.HistorySearchText = "BÀI";

        Assert.Equal(entry, Assert.Single(viewModel.FilteredHistory));
        viewModel.ReAddHistoryCommand.Execute(entry);
        var reAdded = Assert.Single(viewModel.Jobs);
        Assert.Equal(entry.Source, reAdded.Source);
        Assert.Equal(192, reAdded.RequestedBitrate);
        Assert.Contains(reAdded, queue.EnqueuedJobs);

        await viewModel.ClearHistoryCommand.ExecuteAsync();
        Assert.Empty(viewModel.History);
        Assert.Equal(1, history.ClearCount);
    }

    [Fact]
    public void ChooseOutputDirectoryCommand_UsesValidatedDirectoryForNewJobs()
    {
        var folderPicker = new StubFolderPickerService(@"D:\Converted");
        var directoryService = new StubOutputDirectoryService(
            OutputDirectoryValidationResult.Success(@"D:\Converted"));
        var viewModel = CreateViewModel(
            folderPickerService: folderPicker,
            outputDirectoryService: directoryService);

        viewModel.ChooseOutputDirectoryCommand.Execute(null);
        viewModel.SelectedBitrate = "192 kbps";
        viewModel.AddLocalFiles([@"C:\Media\clip.mp4"]);

        var job = Assert.Single(viewModel.Jobs);
        Assert.Equal(@"D:\Converted", viewModel.OutputDirectory);
        Assert.Equal(@"D:\Converted", job.OutputDirectory);
        Assert.Equal(192, job.RequestedBitrate);
        Assert.Empty(viewModel.OutputValidationMessage);
    }

    [Fact]
    public void ChooseOutputDirectoryCommand_KeepsCurrentValueWhenValidationFails()
    {
        var directoryService = new StubOutputDirectoryService(
            OutputDirectoryValidationResult.Failure("Không có quyền ghi."));
        var viewModel = CreateViewModel(
            folderPickerService: new StubFolderPickerService(@"Z:\Protected"),
            outputDirectoryService: directoryService);
        var originalDirectory = viewModel.OutputDirectory;

        viewModel.ChooseOutputDirectoryCommand.Execute(null);

        Assert.Equal(originalDirectory, viewModel.OutputDirectory);
        Assert.Equal("Không có quyền ghi.", viewModel.OutputValidationMessage);
    }

    [Fact]
    public async Task CheckDependenciesAsync_ReportsMissingToolsWithoutThrowing()
    {
        var resolver = new StubMediaToolResolver(
            new MediaToolInfo(
                MediaTool.Ffmpeg,
                "ffmpeg.exe",
                null,
                false,
                null,
                "missing"));
        var viewModel = CreateViewModel(mediaToolResolver: resolver);

        await viewModel.CheckDependenciesAsync();

        Assert.Equal("Thiếu công cụ: ffmpeg.exe", viewModel.DependencyStatus);
    }

    [Fact]
    public void CreateJobProgressReporter_UpdatesObservableJobOnCapturedContext()
    {
        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());
        try
        {
            var viewModel = CreateViewModel();
            viewModel.AddLocalFiles([@"C:\Media\clip.mp4"]);
            var job = Assert.Single(viewModel.Jobs);
            var progress = viewModel.CreateJobProgressReporter(job);

            progress.Report(42.5);

            Assert.Equal(42.5, job.Progress);
            Assert.Equal(ConversionJobStatus.Converting, job.Status);
            Assert.Equal("Đang chuyển đổi", job.CurrentStage);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    [Fact]
    public async Task StartAllAsync_EnqueuesWaitingJobsAndStartsQueue()
    {
        var queue = new StubConversionQueueService();
        var viewModel = CreateViewModel(conversionQueueService: queue);
        viewModel.AddLocalFiles([@"C:\Media\one.mp4", @"C:\Media\two.mp4"]);

        await viewModel.StartAllAsync();

        Assert.Equal(2, queue.EnqueuedJobs.Select(job => job.Id).Distinct().Count());
        Assert.Equal(1, queue.StartCount);
    }

    [Fact]
    public void AggregateProgress_TracksCountsProgressAndActiveJob()
    {
        var queue = new StubConversionQueueService();
        var viewModel = CreateViewModel(conversionQueueService: queue);
        viewModel.AddLocalFiles([
            @"C:\Media\completed.mp4",
            @"C:\Media\failed.mp4",
            @"C:\Media\canceled.mp4",
            @"C:\Media\active.mp4"]);
        viewModel.Jobs[0].Progress = 100;
        viewModel.Jobs[0].Status = ConversionJobStatus.Completed;
        viewModel.Jobs[1].Progress = 40;
        viewModel.Jobs[1].Status = ConversionJobStatus.Failed;
        viewModel.Jobs[2].Progress = 20;
        viewModel.Jobs[2].Status = ConversionJobStatus.Canceled;
        viewModel.Jobs[3].Progress = 60;
        viewModel.Jobs[3].Status = ConversionJobStatus.Converting;
        queue.SetActive(viewModel.Jobs[3]);

        Assert.Equal(1, viewModel.CompletedJobCount);
        Assert.Equal(1, viewModel.FailedJobCount);
        Assert.Equal(1, viewModel.CanceledJobCount);
        Assert.Equal(90, viewModel.OverallProgress);
        Assert.Equal("90%", viewModel.OverallProgressText);
        Assert.Equal("active.mp4", viewModel.ActiveJobName);
        Assert.Equal("Đang xử lý: active.mp4", viewModel.OverallStatus);
        Assert.Equal("1 / 4 hoàn thành · 1 lỗi · 1 đã hủy", viewModel.JobSummary);
    }

    [Fact]
    public void AggregateProgress_ReachesOneHundredWhenEveryJobIsTerminal()
    {
        var viewModel = CreateViewModel();
        viewModel.AddLocalFiles([@"C:\Media\done.mp4", @"C:\Media\failed.mp4"]);
        viewModel.Jobs[0].Status = ConversionJobStatus.Completed;
        viewModel.Jobs[0].Progress = 100;
        viewModel.Jobs[1].Status = ConversionJobStatus.Failed;
        viewModel.Jobs[1].Progress = 35;

        Assert.Equal(100, viewModel.OverallProgress);
        Assert.Equal("100%", viewModel.OverallProgressText);
        Assert.Equal("Đã xử lý xong hàng đợi", viewModel.OverallStatus);
    }

    [Fact]
    public void CancelAll_ForwardsToQueueService()
    {
        var queue = new StubConversionQueueService();
        var viewModel = CreateViewModel(conversionQueueService: queue);

        viewModel.CancelAll();

        Assert.Equal(1, queue.CancelAllCount);
    }

    [Fact]
    public void RetryJobCommand_ResetsFailedJobAndReenqueuesIt()
    {
        var queue = new StubConversionQueueService();
        var viewModel = CreateViewModel(conversionQueueService: queue);
        viewModel.AddLocalFiles([@"C:\Media\failed.mp4"]);
        var job = Assert.Single(viewModel.Jobs);
        job.Status = ConversionJobStatus.Failed;
        job.Progress = 64;
        job.ErrorMessage = "failed";
        job.OutputFilePath = @"C:\Output\failed.mp3";

        Assert.True(viewModel.RetryJobCommand.CanExecute(job));
        viewModel.RetryJobCommand.Execute(job);

        Assert.Equal(ConversionJobStatus.Waiting, job.Status);
        Assert.Equal(0, job.Progress);
        Assert.Null(job.ErrorMessage);
        Assert.Null(job.OutputFilePath);
        Assert.Equal(1, job.RetryCount);
        Assert.Equal(2, queue.EnqueueCallCount);
    }

    [Fact]
    public void RemoveJobCommand_CancelsPendingJobBeforeRemovingIt()
    {
        var queue = new StubConversionQueueService();
        var viewModel = CreateViewModel(conversionQueueService: queue);
        viewModel.AddLocalFiles([@"C:\Media\waiting.mp4"]);
        var job = Assert.Single(viewModel.Jobs);

        viewModel.RemoveJobCommand.Execute(job);

        Assert.Empty(viewModel.Jobs);
        Assert.Equal(1, queue.CancelCount);
    }

    [Fact]
    public void JobInteractionCommands_UseExpectedTargetsAndStatusRules()
    {
        using var directory = new TemporaryDirectory();
        var outputFile = Path.Combine(directory.Path, "result.mp3");
        File.WriteAllText(outputFile, "mp3");
        var interactions = new StubJobInteractionService();
        var viewModel = CreateViewModel(jobInteractionService: interactions);
        viewModel.AddLocalFiles([@"C:\Media\source.mp4"]);
        var job = Assert.Single(viewModel.Jobs);
        job.Status = ConversionJobStatus.Completed;
        job.OutputFilePath = outputFile;

        Assert.True(viewModel.OpenOutputFileCommand.CanExecute(job));
        Assert.True(viewModel.OpenOutputFolderCommand.CanExecute(job));
        Assert.False(viewModel.RetryJobCommand.CanExecute(job));
        Assert.False(viewModel.CancelJobCommand.CanExecute(job));
        viewModel.OpenOutputFileCommand.Execute(job);
        viewModel.OpenOutputFolderCommand.Execute(job);
        viewModel.CopySourceCommand.Execute(job);

        Assert.Equal(outputFile, interactions.OpenedFile);
        Assert.Equal(directory.Path, interactions.OpenedFolder);
        Assert.Equal(job.Source, interactions.CopiedText);

        job.Status = ConversionJobStatus.Failed;
        job.ErrorMessage = "Detailed failure";
        Assert.True(viewModel.ViewErrorCommand.CanExecute(job));
        viewModel.ViewErrorCommand.Execute(job);
        Assert.Equal("Detailed failure", interactions.ErrorMessage);
    }

    private static MainWindowViewModel CreateViewModel(
        IFilePickerService? filePickerService = null,
        IFolderPickerService? folderPickerService = null,
        IOutputDirectoryService? outputDirectoryService = null,
        IMediaToolResolver? mediaToolResolver = null,
        IConversionQueueService? conversionQueueService = null,
        IJobInteractionService? jobInteractionService = null,
        ISettingsService? settingsService = null,
        IHistoryService? historyService = null)
    {
        return new MainWindowViewModel(
            new InputParserService(),
            filePickerService ?? new StubFilePickerService(),
            folderPickerService ?? new StubFolderPickerService(null),
            outputDirectoryService ?? new OutputDirectoryService(),
            mediaToolResolver,
            conversionQueueService,
            jobInteractionService,
            settingsService,
            historyService);
    }

    private sealed class StubHistoryService(params HistoryEntry[] entries) : IHistoryService
    {
        public int ClearCount { get; private set; }
        public Task<IReadOnlyList<HistoryEntry>> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HistoryEntry>>(entries);
        public Task AddAsync(HistoryEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            ClearCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubSettingsService(AppSettings loaded) : ISettingsService
    {
        public int SaveCount { get; private set; }
        public AppSettings? LastSaved { get; private set; }
        public AppSettings Load() => loaded;
        public void Save(AppSettings settings)
        {
            SaveCount++;
            LastSaved = settings;
        }
    }

    private sealed class StubFilePickerService(params string[] filePaths) : IFilePickerService
    {
        public IReadOnlyList<string> PickVideoFiles() => filePaths;
    }

    private sealed class StubFolderPickerService(string? directory) : IFolderPickerService
    {
        public string? PickFolder(string initialDirectory) => directory;
    }

    private sealed class StubOutputDirectoryService(OutputDirectoryValidationResult result)
        : IOutputDirectoryService
    {
        public OutputDirectoryValidationResult ValidateAndCreate(string? directory) => result;
    }

    private sealed class StubMediaToolResolver(params MediaToolInfo[] tools) : IMediaToolResolver
    {
        public string ToolsDirectory => "tools";

        public MediaToolInfo Resolve(MediaTool tool) =>
            tools.Single(item => item.Tool == tool);

        public Task<MediaToolInfo> GetVersionAsync(
            MediaTool tool,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Resolve(tool));

        public Task<IReadOnlyList<MediaToolInfo>> GetDiagnosticsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MediaToolInfo>>(tools);
    }

    private sealed class ImmediateSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state) => callback(state);
    }

    private sealed class StubConversionQueueService : IConversionQueueService
    {
        private readonly HashSet<Guid> _jobIds = [];

        public bool IsRunning { get; private set; }
        public int Concurrency { get; set; } = 2;
        public ConversionJob? ActiveJob { get; private set; }
        public event EventHandler? StateChanged;
        public event EventHandler<ConversionJob>? JobFinished;
        public event EventHandler<PlaylistExpandedEventArgs>? PlaylistExpanded;
        public List<ConversionJob> EnqueuedJobs { get; } = [];
        public int StartCount { get; private set; }
        public int CancelAllCount { get; private set; }
        public int CancelCount { get; private set; }
        public int EnqueueCallCount { get; private set; }

        public void Enqueue(ConversionJob job)
        {
            EnqueueCallCount++;
            if (_jobIds.Add(job.Id))
            {
                EnqueuedJobs.Add(job);
            }
        }

        public void Cancel(ConversionJob job)
        {
            CancelCount++;
            job.Status = ConversionJobStatus.Canceled;
        }

        public void CancelAll()
        {
            CancelAllCount++;
            foreach (var job in EnqueuedJobs.Where(job => job.Status == ConversionJobStatus.Waiting))
            {
                job.Status = ConversionJobStatus.Canceled;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartCount++;
            IsRunning = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
            IsRunning = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public void SetActive(ConversionJob job)
        {
            ActiveJob = job;
            IsRunning = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Finish(ConversionJob job) => JobFinished?.Invoke(this, job);

        public void ExpandPlaylist(
            ConversionJob playlist,
            IReadOnlyList<ConversionJob> items,
            bool wasLimited) =>
            PlaylistExpanded?.Invoke(this, new PlaylistExpandedEventArgs(playlist, items, wasLimited));
    }

    private sealed class StubJobInteractionService : IJobInteractionService
    {
        public string? OpenedFile { get; private set; }
        public string? OpenedFolder { get; private set; }
        public string? CopiedText { get; private set; }
        public string? ErrorMessage { get; private set; }

        public void OpenFile(string filePath) => OpenedFile = filePath;
        public void OpenFolder(string directoryPath) => OpenedFolder = directoryPath;
        public void CopyText(string text) => CopiedText = text;
        public void ShowError(string title, string message) => ErrorMessage = message;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "VideoToMp3.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
