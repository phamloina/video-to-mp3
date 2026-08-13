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
