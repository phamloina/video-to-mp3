using System.IO;
using System.Windows;
using VideoToMp3.App.Services;
using VideoToMp3.App.ViewModels;
using VideoToMp3.Core.Services;

namespace VideoToMp3.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(
            new InputParserService(),
            new FilePickerService(),
            new FolderPickerService(),
            new OutputDirectoryService());
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
