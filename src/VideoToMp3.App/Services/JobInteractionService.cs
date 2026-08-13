using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace VideoToMp3.App.Services;

public sealed class JobInteractionService : IJobInteractionService
{
    public void OpenFile(string filePath) => OpenShellTarget(filePath, "Không thể mở file MP3.");

    public void OpenFolder(string directoryPath) =>
        OpenShellTarget(directoryPath, "Không thể mở thư mục đầu ra.");

    public void CopyText(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception exception) when (exception is System.Runtime.InteropServices.COMException)
        {
            MessageBox.Show(
                $"Không thể sao chép vào clipboard: {exception.Message}",
                "Video To MP3",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    public void ShowError(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool ConfirmCancelAll() =>
        MessageBox.Show(
            "Hủy tất cả tác vụ đang chạy và đang chờ?",
            "Xác nhận hủy",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;

    public void ShowBatchCompleted(int completed, int failed, int canceled) =>
        MessageBox.Show(
            $"Đã hoàn tất hàng đợi.{Environment.NewLine}" +
            $"Thành công: {completed} · Lỗi: {failed} · Đã hủy: {canceled}",
            "Video To MP3",
            MessageBoxButton.OK,
            failed > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

    private static void OpenShellTarget(string target, string errorMessage)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException)
        {
            MessageBox.Show(
                $"{errorMessage}{Environment.NewLine}{exception.Message}",
                "Video To MP3",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
