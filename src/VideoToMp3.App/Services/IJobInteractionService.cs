namespace VideoToMp3.App.Services;

public interface IJobInteractionService
{
    void OpenFile(string filePath);

    void OpenFolder(string directoryPath);

    void CopyText(string text);

    void ShowError(string title, string message);

    bool ConfirmCancelAll();

    Task<bool> ConfirmCloseChromeAndRetryAsync(
        CancellationToken cancellationToken = default);

    void ShowBatchCompleted(int completed, int failed, int canceled);
}
