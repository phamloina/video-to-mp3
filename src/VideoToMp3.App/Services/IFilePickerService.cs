namespace VideoToMp3.App.Services;

public interface IFilePickerService
{
    IReadOnlyList<string> PickVideoFiles();
}
