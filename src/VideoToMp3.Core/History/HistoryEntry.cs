using VideoToMp3.Core.Models;

namespace VideoToMp3.Core.History;

public sealed record HistoryEntry(
    Guid Id,
    ConversionSourceType SourceType,
    string Source,
    string DisplayName,
    string OutputDirectory,
    string? OutputFilePath,
    int RequestedBitrate,
    ConversionJobStatus Status,
    string? ErrorMessage,
    DateTimeOffset CompletedAt)
{
    public static HistoryEntry FromJob(ConversionJob job) => new(
        Guid.NewGuid(),
        job.SourceType,
        job.Source,
        job.DisplayName,
        job.OutputDirectory,
        job.OutputFilePath,
        job.RequestedBitrate,
        job.Status,
        job.ErrorMessage,
        job.CompletedAt ?? DateTimeOffset.UtcNow);
}
