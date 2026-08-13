namespace VideoToMp3.App.Services;

public interface IOutputDirectoryService
{
    OutputDirectoryValidationResult ValidateAndCreate(string? directory);
}
