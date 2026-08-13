using System.IO;
using System.Windows;
using VideoToMp3.App.Services;
using VideoToMp3.App.ViewModels;
using VideoToMp3.Core.Services;
using VideoToMp3.Infrastructure.Dependencies;
using VideoToMp3.Infrastructure.Media;
using VideoToMp3.Infrastructure.Logging;
using VideoToMp3.Infrastructure.History;
using VideoToMp3.Infrastructure.Online;
using VideoToMp3.Infrastructure.Processes;
using VideoToMp3.Infrastructure.Queue;
using VideoToMp3.Infrastructure.Settings;

namespace VideoToMp3.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var processRunner = new ProcessRunner();
        var toolResolver = new MediaToolResolver(AppContext.BaseDirectory, processRunner);
        var probeService = new FfprobeMediaProbeService(toolResolver, processRunner);
        var ffmpegService = new FFmpegService(toolResolver, processRunner: processRunner);
        var ytDlpService = new YtDlpService(toolResolver, processRunner);
        var appLogger = new FileAppLogger();
        var queueService = new ConversionQueueService(
            probeService,
            ffmpegService,
            ytDlpService,
            appLogger);
        DataContext = new MainWindowViewModel(
            new InputParserService(),
            new FilePickerService(),
            new FolderPickerService(),
            new OutputDirectoryService(),
            toolResolver,
            queueService,
            new JobInteractionService(),
            new JsonSettingsService(),
            new JsonHistoryService());
        Loaded += OnWindowLoaded;
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.CheckDependenciesAsync();
            await viewModel.LoadHistoryAsync();
        }
    }

    private void OnWindowDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnWindowDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetData(DataFormats.FileDrop) is not string[] paths ||
            DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.AddLocalFiles(paths.Where(File.Exists));
    }
}
