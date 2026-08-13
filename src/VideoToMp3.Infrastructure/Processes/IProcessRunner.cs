namespace VideoToMp3.Infrastructure.Processes;

public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);

    Task<ProcessRunResult> RunWithProgressAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        IProgress<string> standardOutputProgress,
        CancellationToken cancellationToken = default) =>
        RunAsync(executablePath, arguments, cancellationToken);

    Task<ProcessRunResult> RunWithStandardErrorProgressAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        IProgress<string> standardErrorProgress,
        CancellationToken cancellationToken = default) =>
        RunAsync(executablePath, arguments, cancellationToken);
}
