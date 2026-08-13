using VideoToMp3.Core.Services;

namespace VideoToMp3.Infrastructure.Media;

public sealed class OutputPathResolver : IOutputPathResolver
{
    public string ResolveAvailableMp3Path(string inputFilePath, string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        var baseName = Path.GetFileNameWithoutExtension(inputFilePath);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "audio";
        }

        var candidate = Path.Combine(fullOutputDirectory, $"{baseName}.mp3");
        for (var suffix = 1; File.Exists(candidate); suffix++)
        {
            candidate = Path.Combine(fullOutputDirectory, $"{baseName} ({suffix}).mp3");
        }

        return candidate;
    }
}
