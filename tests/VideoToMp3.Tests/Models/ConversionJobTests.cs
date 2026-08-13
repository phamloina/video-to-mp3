using VideoToMp3.Core.Models;

namespace VideoToMp3.Tests.Models;

public sealed class ConversionJobTests
{
    [Fact]
    public void Constructor_CreatesWaitingJobForLocalFile()
    {
        var job = new ConversionJob(
            ConversionSourceType.LocalFile,
            @"C:\Media\sample.mp4",
            @"C:\Output");

        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal(ConversionJobStatus.Waiting, job.Status);
        Assert.Equal(@"C:\Media\sample.mp4", job.InputFilePath);
        Assert.Null(job.SourceUrl);
        Assert.Equal(320, job.RequestedBitrate);
        Assert.Equal(0, job.Progress);
    }

    [Fact]
    public void Progress_ClampsValueAndNotifiesBindingClients()
    {
        var job = new ConversionJob(
            ConversionSourceType.Url,
            "https://example.com/video",
            @"C:\Output");
        string? changedProperty = null;
        job.PropertyChanged += (_, args) => changedProperty = args.PropertyName;

        job.Progress = 120;

        Assert.Equal(100, job.Progress);
        Assert.Equal(nameof(ConversionJob.Progress), changedProperty);
    }

    [Fact]
    public void Constructor_RejectsInvalidRequiredValues()
    {
        Assert.Throws<ArgumentException>(() =>
            new ConversionJob(ConversionSourceType.Url, " ", @"C:\Output"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ConversionJob(ConversionSourceType.Url, "https://example.com", @"C:\Output", 0));
    }
}
