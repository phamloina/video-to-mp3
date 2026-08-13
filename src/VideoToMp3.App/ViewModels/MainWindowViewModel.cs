using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        IMediaToolResolver? mediaToolResolver = null)
    {
        _inputParserService = inputParserService;
        _filePickerService = filePickerService;
        _folderPickerService = folderPickerService;
        _outputDirectoryService = outputDirectoryService;
        _mediaToolResolver = mediaToolResolver;

        var musicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        _outputDirectory = Path.Combine(musicDirectory, "Video To MP3");

        ChooseFilesCommand = new RelayCommand(ChooseFiles);
        ChooseOutputDirectoryCommand = new RelayCommand(ChooseOutputDirectory);
        AddInputsCommand = new RelayCommand(() => AddInputsFromText());
        Jobs.CollectionChanged += OnJobsCollectionChanged;
    }

    public ObservableCollection<ConversionJob> Jobs { get; } = [];

    public IReadOnlyList<string> BitrateOptions { get; } =
        ["320 kbps", "256 kbps", "192 kbps", "128 kbps"];

    public ICommand ChooseFilesCommand { get; }

    public ICommand ChooseOutputDirectoryCommand { get; }

    public ICommand AddInputsCommand { get; }

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

    public string OverallStatus => IsQueueEmpty ? "Chưa có tác vụ" : "Sẵn sàng chuyển đổi";

    public double OverallProgress => 0;

    public string OverallProgressText => "0%";

    public int AddLocalFiles(IEnumerable<string> filePaths)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        return AddParsedResult(_inputParserService.Parse(string.Join(Environment.NewLine, filePaths)));
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
        OnPropertyChanged(nameof(IsQueueEmpty));
        OnPropertyChanged(nameof(JobSummary));
        OnPropertyChanged(nameof(OverallStatus));
    }
}
