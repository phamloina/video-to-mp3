using VideoToMp3.Core.History;

namespace VideoToMp3.Core.Services;

public interface IHistoryService
{
    Task<IReadOnlyList<HistoryEntry>> LoadAsync(CancellationToken cancellationToken = default);
    Task AddAsync(HistoryEntry entry, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
