namespace VideoToMp3.Core.Media;

public sealed record MediaProbeError(
    MediaProbeErrorCode Code,
    string Message,
    string? TechnicalDetails = null);
