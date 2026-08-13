namespace VideoToMp3.App.Services;

public interface IFolderPickerService
{
    string? PickFolder(string initialDirectory);
}
