using VideoToMp3.Core.Models;
using VideoToMp3.Core.Services;

namespace VideoToMp3.Tests.Services;

public sealed class InputParserServiceTests
{
    private readonly InputParserService _parser = new();

    [Fact]
    public void Parse_RecognizesSingleUrl()
    {
        var result = _parser.Parse("  https://example.com/watch?v=123  ");

        var item = Assert.Single(result.Items);
        Assert.Equal(ConversionSourceType.Url, item.SourceType);
        Assert.Equal("https://example.com/watch?v=123", item.Source);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Parse_RecognizesMultipleUrls()
    {
        var result = _parser.Parse(
            "https://example.com/one\r\nhttps://example.com/two\nhttps://example.com/three");

        Assert.Equal(3, result.Items.Count);
        Assert.All(result.Items, item =>
            Assert.Equal(ConversionSourceType.Url, item.SourceType));
    }

    [Fact]
    public void Parse_RecognizesAbsoluteLocalPathWithoutCheckingFileSystem()
    {
        var result = _parser.Parse(@"C:\Media\missing-video.mp4");

        var item = Assert.Single(result.Items);
        Assert.Equal(ConversionSourceType.LocalFile, item.SourceType);
        Assert.Equal(@"C:\Media\missing-video.mp4", item.Source);
    }

    [Fact]
    public void Parse_RecognizesMixedInput()
    {
        var result = _parser.Parse(
            "https://example.com/video\nC:\\Media\\local.mp4");

        Assert.Collection(
            result.Items,
            item => Assert.Equal(ConversionSourceType.Url, item.SourceType),
            item => Assert.Equal(ConversionSourceType.LocalFile, item.SourceType));
    }

    [Fact]
    public void Parse_RemovesDuplicatesWithinBatch()
    {
        var result = _parser.Parse(
            "https://example.com/video\nhttps://example.com/video\n" +
            "C:\\Media\\clip.mp4\nc:\\media\\CLIP.mp4");

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.DuplicateCount);
    }

    [Fact]
    public void Parse_IgnoresBlankLines()
    {
        var result = _parser.Parse("\r\n   \nhttps://example.com/video\n\t");

        Assert.Single(result.Items);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_PreservesUnicodePath()
    {
        const string unicodePath = @"C:\Video\Nhạc Việt\Bài hát mùa hè.mp4";

        var result = _parser.Parse(unicodePath);

        var item = Assert.Single(result.Items);
        Assert.Equal(unicodePath, item.Source);
        Assert.Equal(ConversionSourceType.LocalFile, item.SourceType);
    }

    [Theory]
    [InlineData("relative\\video.mp4")]
    [InlineData("ftp://example.com/video")]
    [InlineData("not a path or URL")]
    public void Parse_ReportsInvalidInput(string invalidInput)
    {
        var result = _parser.Parse(invalidInput);

        Assert.Empty(result.Items);
        var error = Assert.Single(result.Errors);
        Assert.Equal(invalidInput, error.Input);
        Assert.False(result.IsValid);
    }
}
