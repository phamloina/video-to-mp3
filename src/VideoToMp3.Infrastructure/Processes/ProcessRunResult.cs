namespace VideoToMp3.Infrastructure.Processes;

public sealed record ProcessRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
