using VideoToMp3.Core.Models;

namespace VideoToMp3.Core.Services;

public interface IConversionQueueService
{
    bool IsRunning { get; }

    ConversionJob? ActiveJob { get; }

    event EventHandler? StateChanged;

    void Enqueue(ConversionJob job);

    void Cancel(ConversionJob job);

    void CancelAll();

    Task StartAsync(CancellationToken cancellationToken = default);
}
