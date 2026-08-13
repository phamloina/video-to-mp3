using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using VideoToMp3.App.Commands;
using VideoToMp3.App.Services;
using VideoToMp3.Core.Common;
using VideoToMp3.Core.Inputs;
using VideoToMp3.Core.Models;
using VideoToMp3.Core.Services;

namespace VideoToMp3.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IInputParserService _inputParserService;
    private readonly IFilePickerService _filePickerService;
    private readonly IFolderPickerService _folderPickerService;
    private readonly IOutputDirectoryService _outputDirectoryService;
    private readonly IMediaToolResolver? _mediaToolResolver;
    private readonly IConversionQueueService? _conversionQueueService;
    private readonly IJobInteractionService _jobInteractionService;
    private string _inputText = string.Empty;
    private string _outputDirectory;
    private string _selectedBitrate = "320 kbps";
    private string _inputValidationMessage = string.Empty;
    private string _outputValidationMessage = string.Empty;
    private string _dependencyStatus = "Đang kiểm tra công cụ...";

    public MainWindowViewModel(
        IInputParserService inputParserService,
        IFilePickerService filePickerService,
        IFolderPickerService folderPickerService,
        IOutputDirectoryService outputDirectoryService,
        IMediaToolResolver? mediaToolResolver = null,
        IConversionQueueService? conversionQueueService = null,
        IJobInteractionService? jobInteractionService = null)
    {
        _inputParserService = inputParserService;
        _filePickerService = filePickerService;
        _folderPickerService = folderPickerService;
        _outputDirectoryService = outputDirectoryService;
        _mediaToolResolver = mediaToolResolver;
        _conversionQueueService = conversionQueueService;
        _jobInteractionService = jobInteractionService ?? new JobInteractionService();

        var musicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        _outputDirectory = Path.Combine(musicDirectory, "Video To MP3");

        ChooseFilesCommand = new RelayCommand(ChooseFiles);
        ChooseOutputDirectoryCommand = new RelayCommand(ChooseOutputDirectory);
        AddInputsCommand = new RelayCommand(() => AddInputsFromText());
        StartAllCommand = new AsyncRelayCommand(
            () => StartAllAsync(),
            () => _conversionQueueService is not null &&
                  Jobs.Any(job => job.Status == ConversionJobStatus.Waiting) &&
                  !(_conversionQueueService?.IsRunning ?? false));
        CancelAllCommand = new RelayCommand(
            CancelAll,
            () => _conversionQueueService?.IsRunning == true);
        RetryJobCommand = new RelayCommand<ConversionJob>(RetryJob, CanRetryJob);
        CancelJobCommand = new RelayCommand<ConversionJob>(CancelJob, CanCancelJob);
        RemoveJobCommand = new RelayCommand<ConversionJob>(RemoveJob, CanRemoveJob);
        OpenOutputFileCommand = new RelayCommand<ConversionJob>(OpenOutputFile, CanOpenOutputFile);
        OpenOutputFolderCommand = new RelayCommand<ConversionJob>(OpenOutputFolder, CanOpenOutputFolder);
        CopySourceCommand = new RelayCommand<ConversionJob>(CopySource);
        ViewErrorCommand = new RelayCommand<ConversionJob>(ViewError, CanViewError);
        Jobs.CollectionChanged += OnJobsCollectionChanged;
        if (_conversionQueueService is not null)
        {
            _conversionQueueService.StateChanged += OnQueueStateChanged;
        }
    }

    public ObservableCollection<ConversionJob> Jobs { get; } = [];

    public IReadOnlyList<string> BitrateOptions { get; } =
        ["320 kbps", "256 kbps", "192 kbps", "128 kbps"];

    public ICommand ChooseFilesCommand { get; }

    public ICommand ChooseOutputDirectoryCommand { get; }

    public ICommand AddInputsCommand { get; }

    public AsyncRelayCommand StartAllCommand { get; }

    public RelayCommand CancelAllCommand { get; }

    public RelayCommand<ConversionJob> RetryJobCommand { get; }

    public RelayCommand<ConversionJob> CancelJobCommand { get; }

    public RelayCommand<ConversionJob> RemoveJobCommand { get; }

    public RelayCommand<ConversionJob> OpenOutputFileCommand { get; }

    public RelayCommand<ConversionJob> OpenOutputFolderCommand { get; }

    public RelayCommand<ConversionJob> CopySourceCommand { get; }

    public RelayCommand<ConversionJob> ViewErrorCommand { get; }

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        private set => SetProperty(ref _outputDirectory, value);
    }

    public string SelectedBitrate
    {
        get => _selectedBitrate;
        set => SetProperty(ref _selectedBitrate, value);
    }

    public string InputValidationMessage
    {
        get => _inputValidationMessage;
        private set
        {
            if (SetProperty(ref _inputValidationMessage, value))
            {
                OnPropertyChanged(nameof(HasInputValidationMessage));
            }
        }
    }

    public bool HasInputValidationMessage => InputValidationMessage.Length > 0;

    public string OutputValidationMessage
    {
        get => _outputValidationMessage;
        private set
        {
            if (SetProperty(ref _outputValidationMessage, value))
            {
                OnPropertyChanged(nameof(HasOutputValidationMessage));
            }
        }
    }

    public bool HasOutputValidationMessage => OutputValidationMessage.Length > 0;

    public string DependencyStatus
    {
        get => _dependencyStatus;
        private set => SetProperty(ref _dependencyStatus, value);
    }

    public bool IsQueueEmpty => Jobs.Count == 0;

    public string JobSummary => $"0 / {Jobs.Count} hoàn thành";

    public string OverallStatus => IsQueueEmpty
        ? "Chưa có tác vụ"
        : _conversionQueueService?.IsRunning == true
            ? "Đang chuyển đổi"
            : "Sẵn sàng chuyển đổi";

    public double OverallProgress => 0;

    public string OverallProgressText => "0%";

    public async Task StartAllAsync(CancellationToken cancellationToken = default)
    {
        if (_conversionQueueService is null)
        {
            return;
        }

        foreach (var job in Jobs.Where(job => job.Status == ConversionJobStatus.Waiting))
        {
            _conversionQueueService.Enqueue(job);
        }

        await _conversionQueueService.StartAsync(cancellationToken);
        OnPropertyChanged(nameof(JobSummary));
        OnPropertyChanged(nameof(OverallStatus));
    }

    public void CancelAll() => _conversionQueueService?.CancelAll();

    private void RetryJob(ConversionJob job)
    {
        job.RetryCount++;
        job.Status = ConversionJobStatus.Waiting;
        job.Progress = 0;
        job.CurrentStage = null;
        job.ErrorMessage = null;
        job.StartedAt = null;
        job.CompletedAt = null;
        job.OutputFilePath = null;
        _conversionQueueService?.Enqueue(job);
    }

    private void CancelJob(ConversionJob job) => _conversionQueueService?.Cancel(job);

    private void RemoveJob(ConversionJob job)
    {
        if (job.Status == ConversionJobStatus.Waiting)
        {
            _conversionQueueService?.Cancel(job);
        }

        Jobs.Remove(job);
    }

    private void OpenOutputFile(ConversionJob job) =>
        _jobInteractionService.OpenFile(job.OutputFilePath!);

    private void OpenOutputFolder(ConversionJob job)
    {
        var directory = job.OutputFilePath is not null
            ? Path.GetDirectoryName(job.OutputFilePath)
            : null;
        _jobInteractionService.OpenFolder(directory ?? job.OutputDirectory);
    }

    private void CopySource(ConversionJob job) =>
        _jobInteractionService.CopyText(job.Source);

    private void ViewError(ConversionJob job) =>
        _jobInteractionService.ShowError($"Lỗi - {job.DisplayName}", job.ErrorMessage!);

    private static bool CanRetryJob(ConversionJob job) =>
        job.Status is ConversionJobStatus.Failed or ConversionJobStatus.Canceled;

    private static bool CanCancelJob(ConversionJob job) =>
        job.Status is ConversionJobStatus.Waiting or
            ConversionJobStatus.Analyzing or
            ConversionJobStatus.Downloading or
            ConversionJobStatus.Converting;

    private static bool CanRemoveJob(ConversionJob job) =>
        job.Status is ConversionJobStatus.Waiting or
            ConversionJobStatus.Completed or
            ConversionJobStatus.Failed or
            ConversionJobStatus.Canceled;

    private static bool CanOpenOutputFile(ConversionJob job) =>
        job.Status == ConversionJobStatus.Completed &&
        !string.IsNullOrWhiteSpace(job.OutputFilePath) &&
        File.Exists(job.OutputFilePath);

    private static bool CanOpenOutputFolder(ConversionJob job) =>
        Directory.Exists(job.OutputFilePath is not null
            ? Path.GetDirectoryName(job.OutputFilePath)
            : job.OutputDirectory);

    private static bool CanViewError(ConversionJob job) =>
        job.Status == ConversionJobStatus.Failed &&
        !string.IsNullOrWhiteSpace(job.ErrorMessage);

    public int AddLocalFiles(IEnumerable<string> filePaths)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        return AddParsedResult(_inputParserService.Parse(string.Join(Environment.NewLine, filePaths)));
    }

    public IProgress<double> CreateJobProgressReporter(ConversionJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return new Progress<double>(percentage =>
        {
            job.Status = ConversionJobStatus.Converting;
            job.CurrentStage = "Đang chuyển đổi";
            job.Progress = percentage;
        });
    }

    public async Task CheckDependenciesAsync(CancellationToken cancellationToken = default)
    {
        if (_mediaToolResolver is null)
        {
            DependencyStatus = "Chưa cấu hình kiểm tra công cụ";
            return;
        }

        var diagnostics = await _mediaToolResolver.GetDiagnosticsAsync(cancellationToken);
        var missingTools = diagnostics
            .Where(tool => !tool.IsAvailable)
            .Select(tool => tool.ExecutableName)
            .ToArray();

        DependencyStatus = missingTools.Length == 0
            ? "FFmpeg, ffprobe và yt-dlp sẵn sàng"
            : $"Thiếu công cụ: {string.Join(", ", missingTools)}";
    }

    public int AddInputsFromText()
    {
        var result = _inputParserService.Parse(InputText);
        var addedCount = AddParsedResult(result);

        if (addedCount > 0 && result.Errors.Count == 0)
        {
            InputText = string.Empty;
        }

        return addedCount;
    }

    private void ChooseFiles()
    {
        AddLocalFiles(_filePickerService.PickVideoFiles());
    }

    private void ChooseOutputDirectory()
    {
        var selectedDirectory = _folderPickerService.PickFolder(OutputDirectory);
        if (selectedDirectory is null)
        {
            return;
        }

        var result = _outputDirectoryService.ValidateAndCreate(selectedDirectory);
        if (!result.IsValid || result.DirectoryPath is null)
        {
            OutputValidationMessage = result.ErrorMessage ?? "Thư mục lưu không hợp lệ.";
            return;
        }

        OutputDirectory = result.DirectoryPath;
        OutputValidationMessage = string.Empty;
    }

    private int AddParsedResult(InputParseResult result)
    {
        var addedCount = 0;
        var skippedExistingCount = 0;

        foreach (var item in result.Items)
        {
            if (ContainsSource(item))
            {
                skippedExistingCount++;
                continue;
            }

            var job = new ConversionJob(
                item.SourceType,
                item.Source,
                OutputDirectory,
                GetSelectedBitrate());

            if (item.SourceType == ConversionSourceType.LocalFile)
            {
                job.DisplayName = Path.GetFileName(item.Source);
            }

            Jobs.Add(job);
            _conversionQueueService?.Enqueue(job);
            addedCount++;
        }

        InputValidationMessage = BuildValidationMessage(
            result.Errors,
            result.DuplicateCount + skippedExistingCount);

        return addedCount;
    }

    private bool ContainsSource(ParsedInput item)
    {
        var comparison = item.SourceType == ConversionSourceType.LocalFile
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return Jobs.Any(job =>
            job.SourceType == item.SourceType &&
            string.Equals(job.Source, item.Source, comparison));
    }

    private int GetSelectedBitrate()
    {
        var numericPart = SelectedBitrate.Split(' ', 2)[0];
        return int.TryParse(numericPart, out var bitrate) ? bitrate : 320;
    }

    private static string BuildValidationMessage(
        IReadOnlyList<InputParseError> errors,
        int duplicateCount)
    {
        var messages = errors
            .Take(3)
            .Select(error => $"Không hợp lệ: {error.Input}")
            .ToList();

        if (errors.Count > 3)
        {
            messages.Add($"Và {errors.Count - 3} input không hợp lệ khác.");
        }

        if (duplicateCount > 0)
        {
            messages.Add($"Đã bỏ qua {duplicateCount} nguồn trùng lặp.");
        }

        return string.Join(Environment.NewLine, messages);
    }

    private void OnJobsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ConversionJob job in e.OldItems)
            {
                job.PropertyChanged -= OnJobPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (ConversionJob job in e.NewItems)
            {
                job.PropertyChanged += OnJobPropertyChanged;
            }
        }

        OnPropertyChanged(nameof(IsQueueEmpty));
        OnPropertyChanged(nameof(JobSummary));
        OnPropertyChanged(nameof(OverallStatus));
        StartAllCommand.RaiseCanExecuteChanged();
    }

    private void OnQueueStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(OverallStatus));
        StartAllCommand.RaiseCanExecuteChanged();
        CancelAllCommand.RaiseCanExecuteChanged();
    }

    private void OnJobPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RaiseJobCommandCanExecuteChanged();
        StartAllCommand.RaiseCanExecuteChanged();
    }

    private void RaiseJobCommandCanExecuteChanged()
    {
        RetryJobCommand.RaiseCanExecuteChanged();
        CancelJobCommand.RaiseCanExecuteChanged();
        RemoveJobCommand.RaiseCanExecuteChanged();
        OpenOutputFileCommand.RaiseCanExecuteChanged();
        OpenOutputFolderCommand.RaiseCanExecuteChanged();
        ViewErrorCommand.RaiseCanExecuteChanged();
    }
}
