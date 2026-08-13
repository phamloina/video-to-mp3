using Microsoft.Win32;

namespace VideoToMp3.App.Services;

public sealed class FolderPickerService : IFolderPickerService
{
    public string? PickFolder(string initialDirectory)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Chọn thư mục lưu MP3",
            InitialDirectory = initialDirectory,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
