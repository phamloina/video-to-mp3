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

    public async Task<bool> ConfirmCloseChromeAndRetryAsync(
        CancellationToken cancellationToken = default)
    {
        var processes = Process.GetProcessesByName("chrome");
        if (processes.Length == 0)
        {
            return true;
        }

        var confirmed = MessageBox.Show(
            "Chrome đang khóa cookie cần cho website này. Ứng dụng có thể đóng Chrome rồi tự thử lại.\r\n\r\n" +
            "Hãy lưu biểu mẫu hoặc nội dung chưa gửi trong Chrome trước khi tiếp tục. Các tab thông thường có thể khôi phục khi mở lại Chrome.",
            "Đóng Chrome và thử lại?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
        if (!confirmed)
        {
            DisposeProcesses(processes);
            return false;
        }

        try
        {
            foreach (var process in processes.Where(process => process.MainWindowHandle != IntPtr.Zero))
            {
                process.CloseMainWindow();
            }

            var deadline = DateTime.UtcNow.AddSeconds(8);
            while (DateTime.UtcNow < deadline && HasChromeProcesses())
            {
                await Task.Delay(200, cancellationToken);
            }

            foreach (var process in Process.GetProcessesByName("chrome"))
            {
                using (process)
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(5000);
                    }
                    catch (InvalidOperationException)
                    {
                        // Chrome exited between enumeration and shutdown.
                    }
                }
            }

            return !HasChromeProcesses();
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            MessageBox.Show(
                $"Không thể đóng hoàn toàn Chrome: {exception.Message}\r\nHãy đóng Chrome bằng Task Manager rồi bấm thử lại cho job.",
                "Video To MP3",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }
        finally
        {
            DisposeProcesses(processes);
        }
    }

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

    private static void DisposeProcesses(IEnumerable<Process> processes)
    {
        foreach (var process in processes)
        {
            process.Dispose();
        }
    }

    private static bool HasChromeProcesses()
    {
        var processes = Process.GetProcessesByName("chrome");
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            DisposeProcesses(processes);
        }
    }
}
