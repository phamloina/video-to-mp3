using VideoToMp3.Core.Inputs;
using VideoToMp3.Core.Models;

namespace VideoToMp3.Core.Services;

public sealed class InputParserService : IInputParserService
{
    private static readonly string[] LineSeparators = ["\r\n", "\n", "\r"];

    public InputParseResult Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new InputParseResult([], [], 0);
        }

        var items = new List<ParsedInput>();
        var errors = new List<InputParseError>();
        var urlKeys = new HashSet<string>(StringComparer.Ordinal);
        var localPathKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateCount = 0;

        foreach (var rawLine in input.Split(LineSeparators, StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (TryParseHttpUrl(line, out var urlKey))
            {
                if (!urlKeys.Add(urlKey))
                {
                    duplicateCount++;
                    continue;
                }

                items.Add(new ParsedInput(ConversionSourceType.Url, line));
                continue;
            }

            if (TryParseLocalPath(line, out var localPathKey))
            {
                if (!localPathKeys.Add(localPathKey))
                {
                    duplicateCount++;
                    continue;
                }

                items.Add(new ParsedInput(ConversionSourceType.LocalFile, line));
                continue;
            }

            errors.Add(new InputParseError(
                line,
                "Input must be an absolute local path or an HTTP/HTTPS URL."));
        }

        return new InputParseResult(items, errors, duplicateCount);
    }

    private static bool TryParseHttpUrl(string input, out string normalizedKey)
    {
        normalizedKey = string.Empty;

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        normalizedKey = uri.AbsoluteUri;
        return true;
    }

    private static bool TryParseLocalPath(string input, out string normalizedKey)
    {
        normalizedKey = string.Empty;

        try
        {
            if (!Path.IsPathFullyQualified(input))
            {
                return false;
            }

            normalizedKey = Path.GetFullPath(input);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
