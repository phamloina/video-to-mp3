namespace VideoToMp3.App.Services;

public sealed record OutputDirectoryValidationResult(
    bool IsValid,
    string? DirectoryPath,
    string? ErrorMessage)
{
    public static OutputDirectoryValidationResult Success(string directoryPath) =>
        new(true, directoryPath, null);

    public static OutputDirectoryValidationResult Failure(string errorMessage) =>
        new(false, null, errorMessage);
}
