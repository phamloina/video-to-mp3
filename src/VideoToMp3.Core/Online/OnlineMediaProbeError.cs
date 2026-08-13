namespace VideoToMp3.Core.Online;

public sealed record OnlineMediaProbeError(
    OnlineMediaProbeErrorCode Code,
    string Message,
    string? TechnicalDetails = null);
