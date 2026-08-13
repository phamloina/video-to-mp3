using VideoToMp3.Core.Models;
using VideoToMp3.Core.Online;

namespace VideoToMp3.Core.Services;

public interface IConversionQueueService
{
    bool IsRunning { get; }

    ConversionJob? ActiveJob { get; }
    int Concurrency { get; set; }

    event EventHandler? StateChanged;
    event EventHandler<ConversionJob>? JobFinished;
    event EventHandler<PlaylistExpandedEventArgs>? PlaylistExpanded;

    void Enqueue(ConversionJob job);

    void Cancel(ConversionJob job);

    void CancelAll();

    Task StartAsync(CancellationToken cancellationToken = default);
}
