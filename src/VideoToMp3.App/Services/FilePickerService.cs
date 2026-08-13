using Microsoft.Win32;

namespace VideoToMp3.App.Services;

public sealed class FilePickerService : IFilePickerService
{
    private const string VideoFilter =
        "Video files|*.mp4;*.mkv;*.mov;*.avi;*.webm;*.m4v;*.mpg;*.mpeg;*.ts;*.mts;*.m2ts;*.flv;*.wmv|" +
        "All files|*.*";

    public IReadOnlyList<string> PickVideoFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn video để chuyển đổi",
            Filter = VideoFilter,
            Multiselect = true,
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : [];
    }
}
