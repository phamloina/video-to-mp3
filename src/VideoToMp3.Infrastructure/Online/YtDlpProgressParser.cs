using System.Globalization;

namespace VideoToMp3.Infrastructure.Online;

public sealed class YtDlpProgressParser(IProgress<double> progress)
{
    private const string Prefix = "download:";
    private double _lastProgress;

    public void Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line) ||
            !line.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var value = line[Prefix.Length..].Trim().TrimEnd('%').Trim();
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percentage))
        {
            return;
        }

        percentage = Math.Clamp(percentage, 0, 100);
        if (percentage < _lastProgress)
        {
            return;
        }

        _lastProgress = percentage;
        progress.Report(percentage);
    }
}
