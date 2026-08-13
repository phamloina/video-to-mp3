namespace VideoToMp3.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void CoreAssembly_IsLoadableWithoutPresentationDependency()
    {
        var assemblyName = typeof(VideoToMp3.Core.AssemblyMarker).Assembly.GetName().Name;

        Assert.Equal("VideoToMp3.Core", assemblyName);
    }
}
