using System.Globalization;
using VideoToMp3.App.Converters;
using VideoToMp3.Core.Models;

namespace VideoToMp3.Tests.Converters;

public sealed class FailedJobConverterTests
{
    [Theory]
    [InlineData(ConversionJobStatus.Failed, true)]
    [InlineData(ConversionJobStatus.Completed, false)]
    public void Convert_ReturnsTrueOnlyForFailedStatus(
        ConversionJobStatus status,
        bool expected)
    {
        var result = new FailedJobConverter().Convert(
            status,
            typeof(bool),
            null!,
            CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }
}
