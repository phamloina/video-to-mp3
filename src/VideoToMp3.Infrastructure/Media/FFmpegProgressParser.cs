using System.Diagnostics;
using System.Globalization;

namespace VideoToMp3.Infrastructure.Media;

public sealed class FFmpegProgressParser
{
    private static readonly TimeSpan ReportInterval = TimeSpan.FromMilliseconds(100);
    private readonly TimeSpan _duration;
    private readonly IProgress<double> _progress;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private TimeSpan _lastReportTime = -ReportInterval;
    private double _lastProgress;

    public FFmpegProgressParser(TimeSpan duration, IProgress<double> progress)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        _duration = duration;
        _progress = progress ?? throw new ArgumentNullException(nameof(progress));
    }

    public void Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return;
        }

        var key = line[..separatorIndex];
        var value = line[(separatorIndex + 1)..];
        if (key == "progress" && value == "end")
        {
            Report(100, force: true);
            return;
        }

        if (key == "out_time" &&
            TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var processed))
        {
            var percentage = processed.TotalMilliseconds / _duration.TotalMilliseconds * 100;
            Report(Math.Clamp(percentage, 0, 100), force: false);
        }
    }

    private void Report(double percentage, bool force)
    {
        if (percentage < _lastProgress)
        {
            return;
        }

        if (!force && _stopwatch.Elapsed - _lastReportTime < ReportInterval)
        {
            return;
        }

        _lastProgress = percentage;
        _lastReportTime = _stopwatch.Elapsed;
        _progress.Report(percentage);
    }
}
