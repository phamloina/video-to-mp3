using VideoToMp3.Infrastructure.Online;

namespace VideoToMp3.Tests.Online;

public sealed class YtDlpProgressParserTests
{
    [Fact]
    public void Parse_ReportsInvariantDownloadPercentages()
    {
        var progress = new RecordingProgress();
        var parser = new YtDlpProgressParser(progress);

        parser.Parse("download: 12.5%");
        parser.Parse("download:75.0%");
        parser.Parse("download:100.0%");

        Assert.Equal([12.5, 75, 100], progress.Values);
    }

    [Fact]
    public void Parse_IgnoresMalformedAndRegressingValues()
    {
        var progress = new RecordingProgress();
        var parser = new YtDlpProgressParser(progress);

        parser.Parse("[download] 20%");
        parser.Parse("download:60%");
        parser.Parse("download:not-a-number");
        parser.Parse("download:40%");

        Assert.Equal([60], progress.Values);
    }

    private sealed class RecordingProgress : IProgress<double>
    {
        public List<double> Values { get; } = [];
        public void Report(double value) => Values.Add(value);
    }
}
