using VideoToMp3.Core.History;
using VideoToMp3.Core.Models;
using VideoToMp3.Infrastructure.History;

namespace VideoToMp3.Tests.History;

public sealed class JsonHistoryServiceTests
{
    [Fact]
    public async Task AddLoadAndClearAsync_PersistsOnlyTerminalEntries()
    {
        using var directory = new TemporaryDirectory();
        var service = new JsonHistoryService(directory.Path);
        var completed = CreateEntry(ConversionJobStatus.Completed, "done");
        var waiting = CreateEntry(ConversionJobStatus.Waiting, "waiting");

        await service.AddAsync(completed);
        await service.AddAsync(waiting);
        var loaded = await service.LoadAsync();

        Assert.Equal(completed, Assert.Single(loaded));
        await service.ClearAsync();
        Assert.Empty(await service.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_CorruptHistoryReturnsEmptyWithoutThrowing()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "history.json"), "broken");

        Assert.Empty(await new JsonHistoryService(directory.Path).LoadAsync());
    }

    private static HistoryEntry CreateEntry(ConversionJobStatus status, string name) => new(
        Guid.NewGuid(), ConversionSourceType.LocalFile, $@"C:\Media\{name}.mp4", name,
        @"C:\Output", null, 320, status, null, DateTimeOffset.UtcNow);

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VideoToMp3.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }
        public string Path { get; }
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
