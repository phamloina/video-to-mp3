using System.IO;

namespace VideoToMp3.App.Services;

public sealed class OutputDirectoryService : IOutputDirectoryService
{
    public OutputDirectoryValidationResult ValidateAndCreate(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return OutputDirectoryValidationResult.Failure(
                "Vui lòng chọn thư mục lưu hợp lệ.");
        }

        try
        {
            var fullPath = Path.GetFullPath(directory.Trim());
            Directory.CreateDirectory(fullPath);

            return OutputDirectoryValidationResult.Success(fullPath);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException or
            UnauthorizedAccessException or IOException)
        {
            return OutputDirectoryValidationResult.Failure(
                $"Không thể sử dụng thư mục lưu: {exception.Message}");
        }
    }
}
