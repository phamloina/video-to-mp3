namespace VideoToMp3.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void CoreAssembly_IsLoadableWithoutPresentationDependency()
    {
        var coreAssembly = typeof(VideoToMp3.Core.AssemblyMarker).Assembly;
        var assemblyName = coreAssembly.GetName().Name;
        var referencedAssemblies = coreAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Equal("VideoToMp3.Core", assemblyName);
        Assert.DoesNotContain("PresentationFramework", referencedAssemblies);
        Assert.DoesNotContain("PresentationCore", referencedAssemblies);
    }
}
