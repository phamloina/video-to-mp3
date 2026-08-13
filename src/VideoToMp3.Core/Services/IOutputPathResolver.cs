namespace VideoToMp3.Core.Services;

public interface IOutputPathResolver
{
    string SanitizeFileName(string fileName);

    string ResolveAvailableMp3Path(
        string inputFilePath,
        string outputDirectory,
        string? preferredBaseName = null);
}
