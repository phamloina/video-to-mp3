using System.Text;
using VideoToMp3.Infrastructure.Media;

namespace VideoToMp3.Tests.Media;

public sealed class OutputPathResolverTests
{
    [Fact]
    public void ResolveAvailableMp3Path_AddsSuffixWithoutOverwriting()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "Bài hát.mp3"), "existing");
        var resolver = new OutputPathResolver();

        var result = resolver.ResolveAvailableMp3Path(
            Path.Combine(directory.Path, "Bài hát.mp4"),
            directory.Path);

        Assert.Equal(Path.Combine(directory.Path, "Bài hát (1).mp3"), result);
    }

    [Theory]
    [InlineData("video<>:\"/\\|?*", "video_________")]
    [InlineData("title.   ", "title")]
    [InlineData("   ", "audio")]
    [InlineData("CON", "_CON")]
    [InlineData("CON.txt", "_CON.txt")]
    [InlineData("lpt9", "_lpt9")]
    public void SanitizeFileName_ProducesWindowsSafeName(string input, string expected)
    {
        Assert.Equal(expected, new OutputPathResolver().SanitizeFileName(input));
    }

    [Fact]
    public void SanitizeFileName_NormalizesUnicodeAndPreservesEmoji()
    {
        var decomposed = "Ba\u0300i ha\u0301t 🎵";

        var result = new OutputPathResolver().SanitizeFileName(decomposed);

        Assert.Equal("Bài hát 🎵", result);
        Assert.True(result.IsNormalized(NormalizationForm.FormC));
    }

    [Fact]
    public void ResolveAvailableMp3Path_UsesSanitizedOnlineTitleAndLimitsLength()
    {
        using var directory = new TemporaryDirectory();
        var title = new string('A', 200) + "?";
        var resolver = new OutputPathResolver();

        var first = resolver.ResolveAvailableMp3Path("source.webm", directory.Path, title);
        File.WriteAllText(first, "existing");
        var second = resolver.ResolveAvailableMp3Path("source.webm", directory.Path, title);

        Assert.Equal(124, Path.GetFileName(first).Length);
        Assert.EndsWith(" (1).mp3", second);
        Assert.True(Path.GetFileNameWithoutExtension(second).Length <= 120);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "VideoToMp3.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
