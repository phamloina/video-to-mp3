using System.Text.Json;
using VideoToMp3.Core.History;
using VideoToMp3.Core.Models;
using VideoToMp3.Core.Services;

namespace VideoToMp3.Infrastructure.History;

public sealed class JsonHistoryService : IHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;

    public JsonHistoryService(string? dataDirectory = null)
    {
        var directory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoToMp3");
        _filePath = Path.Combine(directory, "history.json");
    }

    public async Task<IReadOnlyList<HistoryEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReadUnsafeAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(HistoryEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Status is not (ConversionJobStatus.Completed or ConversionJobStatus.Failed or ConversionJobStatus.Canceled))
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = (await ReadUnsafeAsync(cancellationToken).ConfigureAwait(false)).ToList();
            entries.Insert(0, entry);
            await WriteUnsafeAsync(entries.Take(500), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // History persistence is best effort and must not interrupt conversion.
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(_filePath)) File.Delete(_filePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Keep the in-memory UI usable even if the history file cannot be removed.
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<HistoryEntry>> ReadUnsafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_filePath)) return [];
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<List<HistoryEntry>>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    private async Task WriteUnsafeAsync(IEnumerable<HistoryEntry> entries, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, entries, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
