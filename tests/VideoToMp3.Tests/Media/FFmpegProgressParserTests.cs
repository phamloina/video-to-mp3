using VideoToMp3.Infrastructure.Media;

namespace VideoToMp3.Tests.Media;

public sealed class FFmpegProgressParserTests
{
    [Fact]
    public void Parse_ReportsProcessedTimeAsClampedPercentage()
    {
        var progress = new RecordingProgress<double>();
        var parser = new FFmpegProgressParser(TimeSpan.FromSeconds(10), progress);

        parser.Parse("out_time=00:00:05.000000");
        parser.Parse("out_time=00:00:12.000000");
        parser.Parse("progress=end");

        Assert.Equal(50, progress.Values[0]);
        Assert.Equal(100, progress.Values[^1]);
        Assert.All(progress.Values, value => Assert.InRange(value, 0, 100));
    }

    [Fact]
    public void Parse_IgnoresMalformedAndRegressingValues()
    {
        var progress = new RecordingProgress<double>();
        var parser = new FFmpegProgressParser(TimeSpan.FromSeconds(10), progress);

        parser.Parse("not-progress");
        parser.Parse("out_time=00:00:06.000000");
        parser.Parse("out_time=00:00:02.000000");

        Assert.Single(progress.Values);
        Assert.Equal(60, progress.Values[0]);
    }

    private sealed class RecordingProgress<T> : IProgress<T>
    {
        public List<T> Values { get; } = [];
        public void Report(T value) => Values.Add(value);
    }
}
