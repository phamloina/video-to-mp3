using System.Diagnostics;
using VideoToMp3.Infrastructure.Processes;

namespace VideoToMp3.Tests.Processes;

public sealed class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_CancellationTerminatesProcessPromptly()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        var stopwatch = Stopwatch.StartNew();
        var runner = new ProcessRunner();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            runner.RunAsync(
                "powershell.exe",
                ["-NoProfile", "-NonInteractive", "-Command", "Start-Sleep -Seconds 30"],
                cancellation.Token));

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5));
    }
}
