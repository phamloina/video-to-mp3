namespace VideoToMp3.Core.Services;

public interface IOutputPathResolver
{
    string ResolveAvailableMp3Path(string inputFilePath, string outputDirectory);
}
