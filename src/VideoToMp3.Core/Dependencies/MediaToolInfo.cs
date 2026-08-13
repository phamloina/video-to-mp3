namespace VideoToMp3.Core.Dependencies;

public sealed record MediaToolInfo(
    MediaTool Tool,
    string ExecutableName,
    string? ExecutablePath,
    bool IsAvailable,
    string? Version,
    string? ErrorMessage);
