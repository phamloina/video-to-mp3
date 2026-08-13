using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using VideoToMp3.App.Commands;
using VideoToMp3.App.Services;
using VideoToMp3.Core.Common;
using VideoToMp3.Core.Inputs;
using VideoToMp3.Core.History;
using VideoToMp3.Core.Models;
using VideoToMp3.Core.Online;
using VideoToMp3.Core.Services;
using VideoToMp3.Core.Settings;

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
    private readonly ISettingsService? _settingsService;
    private readonly IHistoryService? _historyService;
    private readonly IThemeService? _themeService;
    private string _inputText = string.Empty;
    private string _outputDirectory;
    private string _selectedBitrate = "320 kbps";
    private string _inputValidationMessage = string.Empty;
    private string _outputValidationMessage = string.Empty;
    private string _dependencyStatus = "Đang kiểm tra công cụ...";
    private int _concurrency = 2;
    private string _selectedTheme = "System";
    private bool _notificationsEnabled = true;
    private bool _embedThumbnail = true;
    private bool _isInitialized;
    private string _historySearchText = string.Empty;

    public MainWindowViewModel(
        IInputParserService inputParserService,
        IFilePickerService filePickerService,
        IFolderPickerService folderPickerService,
        IOutputDirectoryService outputDirectoryService,
        IMediaToolResolver? mediaToolResolver = null,
        IConversionQueueService? conversionQueueService = null,
        IJobInteractionService? jobInteractionService = null,
        ISettingsService? settingsService = null,
        IHistoryService? historyService = null,
        IThemeService? themeService = null)
    {
        _inputParserService = inputParserService;
        _filePickerService = filePickerService;
        _folderPickerService = folderPickerService;
        _outputDirectoryService = outputDirectoryService;
        _mediaToolResolver = mediaToolResolver;
        _conversionQueueService = conversionQueueService;
        _jobInteractionService = jobInteractionService ?? new JobInteractionService();
        _settingsService = settingsService;
        _historyService = historyService;
        _themeService = themeService;

        var musicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        var defaults = new AppSettings(OutputDirectory: Path.Combine(musicDirectory, "Video To MP3"));
        var settings = _settingsService?.Load() ?? defaults;
        _outputDirectory = settings.OutputDirectory ?? defaults.OutputDirectory!;
        _selectedBitrate = $"{settings.Bitrate} kbps";
        if (!BitrateOptions.Contains(_selectedBitrate))
        {
            _selectedBitrate = "320 kbps";
        }

        _concurrency = Math.Clamp(settings.Concurrency, 1, 4);
        if (_conversionQueueService is not null)
        {
            _conversionQueueService.Concurrency = _concurrency;
        }
        _selectedTheme = settings.Theme is "Light" or "Dark" or "System"
            ? settings.Theme
            : "System";
        _themeService?.Apply(_selectedTheme);
        _notificationsEnabled = settings.NotificationsEnabled;
        _embedThumbnail = settings.EmbedThumbnail;

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
        ClearCompletedCommand = new RelayCommand(
            ClearCompleted,
            () => Jobs.Any(job => job.Status == ConversionJobStatus.Completed));
        OpenOutputDirectoryCommand = new RelayCommand(
            () => _jobInteractionService.OpenFolder(OutputDirectory),
            () => Directory.Exists(OutputDirectory));
        RetryJobCommand = new RelayCommand<ConversionJob>(RetryJob, CanRetryJob);
        CancelJobCommand = new RelayCommand<ConversionJob>(CancelJob, CanCancelJob);
        RemoveJobCommand = new RelayCommand<ConversionJob>(RemoveJob, CanRemoveJob);
        OpenOutputFileCommand = new RelayCommand<ConversionJob>(OpenOutputFile, CanOpenOutputFile);
        OpenOutputFolderCommand = new RelayCommand<ConversionJob>(OpenOutputFolder, CanOpenOutputFolder);
        CopySourceCommand = new RelayCommand<ConversionJob>(CopySource);
        ViewErrorCommand = new RelayCommand<ConversionJob>(ViewError, CanViewError);
        OpenHistoryFileCommand = new RelayCommand<HistoryEntry>(OpenHistoryFile, CanOpenHistoryFile);
        OpenHistoryFolderCommand = new RelayCommand<HistoryEntry>(OpenHistoryFolder);
        ReAddHistoryCommand = new RelayCommand<HistoryEntry>(ReAddHistory);
        ClearHistoryCommand = new AsyncRelayCommand(ClearHistoryAsync, () => History.Count > 0);
        Jobs.CollectionChanged += OnJobsCollectionChanged;
        if (_conversionQueueService is not null)
        {
            _conversionQueueService.StateChanged += OnQueueStateChanged;
            _conversionQueueService.JobFinished += OnJobFinished;
            _conversionQueueService.PlaylistExpanded += OnPlaylistExpanded;
        }

        _isInitialized = true;
    }

    public ObservableCollection<ConversionJob> Jobs { get; } = [];
    public ObservableCollection<HistoryEntry> History { get; } = [];

    public IReadOnlyList<string> BitrateOptions { get; } =
        ["320 kbps", "256 kbps", "192 kbps", "128 kbps"];

    public IReadOnlyList<string> ThemeOptions { get; } = ["System", "Light", "Dark"];

    public ICommand ChooseFilesCommand { get; }

    public ICommand ChooseOutputDirectoryCommand { get; }

    public ICommand AddInputsCommand { get; }

    public AsyncRelayCommand StartAllCommand { get; }

    public RelayCommand CancelAllCommand { get; }
    public RelayCommand ClearCompletedCommand { get; }
    public RelayCommand OpenOutputDirectoryCommand { get; }

    public RelayCommand<ConversionJob> RetryJobCommand { get; }

    public RelayCommand<ConversionJob> CancelJobCommand { get; }

    public RelayCommand<ConversionJob> RemoveJobCommand { get; }

    public RelayCommand<ConversionJob> OpenOutputFileCommand { get; }

    public RelayCommand<ConversionJob> OpenOutputFolderCommand { get; }

    public RelayCommand<ConversionJob> CopySourceCommand { get; }

    public RelayCommand<ConversionJob> ViewErrorCommand { get; }
    public RelayCommand<HistoryEntry> OpenHistoryFileCommand { get; }
    public RelayCommand<HistoryEntry> OpenHistoryFolderCommand { get; }
    public RelayCommand<HistoryEntry> ReAddHistoryCommand { get; }
    public AsyncRelayCommand ClearHistoryCommand { get; }

    public string HistorySearchText
    {
        get => _historySearchText;
        set
        {
            if (SetProperty(ref _historySearchText, value))
            {
                OnPropertyChanged(nameof(FilteredHistory));
            }
        }
    }

    public IEnumerable<HistoryEntry> FilteredHistory => string.IsNullOrWhiteSpace(HistorySearchText)
        ? History
        : History.Where(entry =>
            entry.DisplayName.Contains(HistorySearchText, StringComparison.OrdinalIgnoreCase) ||
            entry.Source.Contains(HistorySearchText, StringComparison.OrdinalIgnoreCase) ||
            (entry.OutputFilePath?.Contains(HistorySearchText, StringComparison.OrdinalIgnoreCase) ?? false));

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        private set
        {
            if (SetProperty(ref _outputDirectory, value)) SaveSettings();
        }
    }

    public string SelectedBitrate
    {
        get => _selectedBitrate;
        set
        {
            if (SetProperty(ref _selectedBitrate, value)) SaveSettings();
        }
    }

    public int Concurrency
    {
        get => _concurrency;
        set
        {
            var normalized = Math.Clamp(value, 1, 4);
            if (SetProperty(ref _concurrency, normalized))
            {
                if (_conversionQueueService is not null)
                {
                    _conversionQueueService.Concurrency = normalized;
                }
                SaveSettings();
            }
        }
    }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            var normalized = value is "Light" or "Dark" or "System" ? value : "System";
            if (SetProperty(ref _selectedTheme, normalized))
            {
                _themeService?.Apply(normalized);
                SaveSettings();
            }
        }
    }

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set
        {
            if (SetProperty(ref _notificationsEnabled, value)) SaveSettings();
        }
    }

    public bool EmbedThumbnail
    {
        get => _embedThumbnail;
        set
        {
            if (SetProperty(ref _embedThumbnail, value)) SaveSettings();
        }
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

    public int CompletedJobCount => Jobs.Count(job => job.Status == ConversionJobStatus.Completed);

    public int FailedJobCount => Jobs.Count(job => job.Status == ConversionJobStatus.Failed);

    public int CanceledJobCount => Jobs.Count(job => job.Status == ConversionJobStatus.Canceled);

    public string? ActiveJobName => _conversionQueueService?.ActiveJob?.DisplayName;

    public string JobSummary =>
        $"{CompletedJobCount} / {Jobs.Count} hoàn thành · {FailedJobCount} lỗi · {CanceledJobCount} đã hủy";

    public string OverallStatus => IsQueueEmpty
        ? "Chưa có tác vụ"
        : _conversionQueueService?.IsRunning == true
            ? ActiveJobName is { Length: > 0 } activeJob
                ? $"Đang xử lý: {activeJob}"
                : "Đang chuẩn bị hàng đợi"
            : Jobs.All(job => job.Status is
                ConversionJobStatus.Completed or
                ConversionJobStatus.Failed or
                ConversionJobStatus.Canceled)
                ? "Đã xử lý xong hàng đợi"
                : "Sẵn sàng chuyển đổi";

    public double OverallProgress => Jobs.Count == 0
        ? 0
        : Jobs.Average(job => job.Status is
            ConversionJobStatus.Completed or
            ConversionJobStatus.Failed or
            ConversionJobStatus.Canceled
                ? 100
                : job.Progress);

    public string OverallProgressText => $"{OverallProgress:0}%";

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
        RaiseAggregatePropertiesChanged();
        if (NotificationsEnabled)
        {
            _jobInteractionService.ShowBatchCompleted(
                CompletedJobCount,
                FailedJobCount,
                CanceledJobCount);
        }
    }

    public void CancelAll()
    {
        if (_conversionQueueService?.IsRunning != true ||
            !_jobInteractionService.ConfirmCancelAll())
        {
            return;
        }

        _conversionQueueService.CancelAll();
    }

    private void ClearCompleted()
    {
        foreach (var job in Jobs
                     .Where(job => job.Status == ConversionJobStatus.Completed)
                     .ToArray())
        {
            Jobs.Remove(job);
        }
    }

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

    public async Task LoadHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (_historyService is null) return;
        var entries = await _historyService.LoadAsync(cancellationToken);
        History.Clear();
        foreach (var entry in entries) History.Add(entry);
        OnPropertyChanged(nameof(FilteredHistory));
        ClearHistoryCommand.RaiseCanExecuteChanged();
        ClearCompletedCommand.RaiseCanExecuteChanged();
        OpenOutputDirectoryCommand.RaiseCanExecuteChanged();
    }

    private async void OnJobFinished(object? sender, ConversionJob job)
    {
        if (_historyService is null) return;
        var entry = HistoryEntry.FromJob(job);
        try
        {
            await _historyService.AddAsync(entry);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }
        void AddToView()
        {
            History.Insert(0, entry);
            OnPropertyChanged(nameof(FilteredHistory));
            ClearHistoryCommand.RaiseCanExecuteChanged();
        }

        if (System.Windows.Application.Current?.Dispatcher is { } dispatcher)
        {
            await dispatcher.InvokeAsync(AddToView);
        }
        else
        {
            AddToView();
        }
    }

    private void OnPlaylistExpanded(object? sender, PlaylistExpandedEventArgs args)
    {
        void UpdateView()
        {
            Jobs.Remove(args.PlaylistJob);
            foreach (var itemJob in args.ItemJobs)
            {
                Jobs.Add(itemJob);
            }

            InputValidationMessage = args.WasLimited
                ? $"Đã mở rộng playlist thành {args.ItemJobs.Count} mục (giới hạn an toàn 100)."
                : $"Đã mở rộng playlist thành {args.ItemJobs.Count} mục.";
        }

        if (System.Windows.Application.Current?.Dispatcher is { } dispatcher &&
            !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(UpdateView);
        }
        else
        {
            UpdateView();
        }
    }

    private void OpenHistoryFile(HistoryEntry entry) => _jobInteractionService.OpenFile(entry.OutputFilePath!);
    private static bool CanOpenHistoryFile(HistoryEntry entry) =>
        !string.IsNullOrWhiteSpace(entry.OutputFilePath) && File.Exists(entry.OutputFilePath);
    private void OpenHistoryFolder(HistoryEntry entry) =>
        _jobInteractionService.OpenFolder(
            entry.OutputFilePath is null ? entry.OutputDirectory : Path.GetDirectoryName(entry.OutputFilePath)!);

    private void ReAddHistory(HistoryEntry entry)
    {
        var job = new ConversionJob(entry.SourceType, entry.Source, OutputDirectory, entry.RequestedBitrate)
        {
            DisplayName = entry.DisplayName
        };
        Jobs.Add(job);
        _conversionQueueService?.Enqueue(job);
    }

    private async Task ClearHistoryAsync()
    {
        if (_historyService is null) return;
        await _historyService.ClearAsync();
        History.Clear();
        OnPropertyChanged(nameof(FilteredHistory));
        ClearHistoryCommand.RaiseCanExecuteChanged();
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
                GetSelectedBitrate())
            {
                EmbedThumbnail = EmbedThumbnail
            };

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

    private void SaveSettings()
    {
        if (!_isInitialized || _settingsService is null)
        {
            return;
        }

        _settingsService.Save(new AppSettings(
            OutputDirectory,
            GetSelectedBitrate(),
            Concurrency,
            SelectedTheme,
            NotificationsEnabled,
            EmbedThumbnail));
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

        RaiseAggregatePropertiesChanged();
        StartAllCommand.RaiseCanExecuteChanged();
        ClearCompletedCommand.RaiseCanExecuteChanged();
    }

    private void OnQueueStateChanged(object? sender, EventArgs e)
    {
        RaiseAggregatePropertiesChanged();
        StartAllCommand.RaiseCanExecuteChanged();
        CancelAllCommand.RaiseCanExecuteChanged();
        ClearCompletedCommand.RaiseCanExecuteChanged();
    }

    private void OnJobPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is
            nameof(ConversionJob.Status) or
            nameof(ConversionJob.Progress) or
            nameof(ConversionJob.DisplayName) or
            nameof(ConversionJob.CurrentStage))
        {
            RaiseAggregatePropertiesChanged();
        }

        RaiseJobCommandCanExecuteChanged();
        StartAllCommand.RaiseCanExecuteChanged();
        ClearCompletedCommand.RaiseCanExecuteChanged();
    }

    private void RaiseAggregatePropertiesChanged()
    {
        OnPropertyChanged(nameof(IsQueueEmpty));
        OnPropertyChanged(nameof(CompletedJobCount));
        OnPropertyChanged(nameof(FailedJobCount));
        OnPropertyChanged(nameof(CanceledJobCount));
        OnPropertyChanged(nameof(ActiveJobName));
        OnPropertyChanged(nameof(JobSummary));
        OnPropertyChanged(nameof(OverallStatus));
        OnPropertyChanged(nameof(OverallProgress));
        OnPropertyChanged(nameof(OverallProgressText));
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
