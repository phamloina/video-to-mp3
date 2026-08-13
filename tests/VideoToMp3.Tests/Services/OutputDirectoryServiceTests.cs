using VideoToMp3.App.Services;

namespace VideoToMp3.Tests.Services;

public sealed class OutputDirectoryServiceTests
{
    [Fact]
    public void ValidateAndCreate_CreatesMissingDirectory()
    {
        var baseDirectory = Path.Combine(
            Path.GetTempPath(),
            "VideoToMp3.Tests",
            Guid.NewGuid().ToString("N"));
        var targetDirectory = Path.Combine(baseDirectory, "Output");

        try
        {
            var result = new OutputDirectoryService().ValidateAndCreate(targetDirectory);

            Assert.True(result.IsValid);
            Assert.Equal(Path.GetFullPath(targetDirectory), result.DirectoryPath);
            Assert.True(Directory.Exists(targetDirectory));
        }
        finally
        {
            if (Directory.Exists(baseDirectory))
            {
                Directory.Delete(baseDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void ValidateAndCreate_RejectsBlankDirectory()
    {
        var result = new OutputDirectoryService().ValidateAndCreate(" ");

        Assert.False(result.IsValid);
        Assert.Null(result.DirectoryPath);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public void ValidateAndCreate_RejectsInvalidPathWithoutThrowing()
    {
        var result = new OutputDirectoryService().ValidateAndCreate("invalid\0path");

        Assert.False(result.IsValid);
        Assert.Null(result.DirectoryPath);
        Assert.Contains("thư mục", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
