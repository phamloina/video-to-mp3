using VideoToMp3.App.Services;
using VideoToMp3.App.ViewModels;
using VideoToMp3.Core.Dependencies;
using VideoToMp3.Core.Models;
using VideoToMp3.Core.Services;

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
        Assert.Equal("0 / 2 hoàn thành", viewModel.JobSummary);
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

    private static MainWindowViewModel CreateViewModel(
        IFilePickerService? filePickerService = null,
        IFolderPickerService? folderPickerService = null,
        IOutputDirectoryService? outputDirectoryService = null,
        IMediaToolResolver? mediaToolResolver = null,
        IConversionQueueService? conversionQueueService = null)
    {
        return new MainWindowViewModel(
            new InputParserService(),
            filePickerService ?? new StubFilePickerService(),
            folderPickerService ?? new StubFolderPickerService(null),
            outputDirectoryService ?? new OutputDirectoryService(),
            mediaToolResolver,
            conversionQueueService);
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
        public event EventHandler? StateChanged;
        public List<ConversionJob> EnqueuedJobs { get; } = [];
        public int StartCount { get; private set; }

        public void Enqueue(ConversionJob job)
        {
            if (_jobIds.Add(job.Id))
            {
                EnqueuedJobs.Add(job);
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
    }
}
