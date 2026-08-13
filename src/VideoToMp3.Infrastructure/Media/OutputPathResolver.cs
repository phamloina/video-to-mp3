using VideoToMp3.Core.Services;
using System.Text;

namespace VideoToMp3.Infrastructure.Media;

public sealed class OutputPathResolver : IOutputPathResolver
{
    private const int MaximumBaseNameLength = 120;
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public string SanitizeFileName(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        var normalized = fileName.Normalize(NormalizationForm.FormC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            builder.Append(character < 32 || character is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*'
                ? '_'
                : character);
        }

        var sanitized = builder.ToString().Trim().TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "audio";
        }

        var firstDot = sanitized.IndexOf('.');
        var deviceName = firstDot >= 0 ? sanitized[..firstDot] : sanitized;
        if (ReservedNames.Contains(deviceName))
        {
            sanitized = $"_{sanitized}";
        }

        return TruncateWithoutSplittingSurrogatePair(sanitized, MaximumBaseNameLength);
    }

    public string ResolveAvailableMp3Path(
        string inputFilePath,
        string outputDirectory,
        string? preferredBaseName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        var sourceName = string.IsNullOrWhiteSpace(preferredBaseName)
            ? Path.GetFileNameWithoutExtension(inputFilePath)
            : preferredBaseName;
        var baseName = SanitizeFileName(sourceName ?? string.Empty);

        var candidate = Path.Combine(fullOutputDirectory, $"{baseName}.mp3");
        for (var suffix = 1; File.Exists(candidate); suffix++)
        {
            var suffixText = $" ({suffix})";
            var availableLength = MaximumBaseNameLength - suffixText.Length;
            var suffixedBaseName = TruncateWithoutSplittingSurrogatePair(baseName, availableLength);
            candidate = Path.Combine(fullOutputDirectory, $"{suffixedBaseName}{suffixText}.mp3");
        }

        return candidate;
    }

    private static string TruncateWithoutSplittingSurrogatePair(string value, int maximumLength)
    {
        if (value.Length <= maximumLength)
        {
            return value;
        }

        var length = maximumLength;
        if (length > 0 && char.IsHighSurrogate(value[length - 1]))
        {
            length--;
        }

        return value[..length];
    }
}
