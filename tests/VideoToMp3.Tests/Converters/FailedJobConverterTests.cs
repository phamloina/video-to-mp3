using System.Globalization;
using VideoToMp3.App.Converters;
using VideoToMp3.Core.Models;

namespace VideoToMp3.Tests.Converters;

public sealed class FailedJobConverterTests
{
    [Theory]
    [InlineData(ConversionJobStatus.Waiting, false)]
    [InlineData(ConversionJobStatus.Analyzing, false)]
    [InlineData(ConversionJobStatus.Downloading, false)]
    [InlineData(ConversionJobStatus.Converting, false)]
    [InlineData(ConversionJobStatus.Expanded, false)]
    [InlineData(ConversionJobStatus.Failed, true)]
    [InlineData(ConversionJobStatus.Completed, false)]
    [InlineData(ConversionJobStatus.Canceled, false)]
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

    [Theory]
    [InlineData(null)]
    [InlineData("Failed")]
    [InlineData(6)]
    public void Convert_ReturnsFalseForUnexpectedBindings(object? value)
    {
        var result = new FailedJobConverter().Convert(
            value!,
            typeof(bool),
            null!,
            CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => new FailedJobConverter().ConvertBack(
            true,
            typeof(ConversionJobStatus),
            null!,
            CultureInfo.InvariantCulture));
    }
}
