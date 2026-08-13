using VideoToMp3.Core.Dependencies;

namespace VideoToMp3.Core.Services;

public interface IMediaToolResolver
{
    string ToolsDirectory { get; }

    MediaToolInfo Resolve(MediaTool tool);

    Task<MediaToolInfo> EnsureAvailableAsync(
        MediaTool tool,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Resolve(tool));

    Task<MediaToolInfo> GetVersionAsync(
        MediaTool tool,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaToolInfo>> GetDiagnosticsAsync(
        CancellationToken cancellationToken = default);
}
