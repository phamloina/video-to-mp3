using VideoToMp3.Core.Common;

namespace VideoToMp3.Core.Models;

public sealed class ConversionJob : ObservableObject
{
    private string _displayName;
    private string _outputDirectory;
    private string? _outputFilePath;
    private int _requestedBitrate;
    private ConversionJobStatus _status = ConversionJobStatus.Waiting;
    private double _progress;
    private string? _currentStage;
    private string? _errorMessage;
    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _completedAt;
    private TimeSpan? _duration;
    private string? _thumbnailUrl;
    private string? _thumbnailLocalPath;
    private MediaMetadata? _metadata;
    private bool _embedThumbnail = true;
    private int _retryCount;

    public ConversionJob(
        ConversionSourceType sourceType,
        string source,
        string outputDirectory,
        int requestedBitrate = 320)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        if (requestedBitrate <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedBitrate),
                "Bitrate must be greater than zero.");
        }

        Id = Guid.NewGuid();
        SourceType = sourceType;
        Source = source;
        _displayName = source;
        _outputDirectory = outputDirectory;
        _requestedBitrate = requestedBitrate;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public ConversionSourceType SourceType { get; }

    public string Source { get; }

    public string? InputFilePath =>
        SourceType == ConversionSourceType.LocalFile ? Source : null;

    public string? SourceUrl =>
        SourceType == ConversionSourceType.Url ? Source : null;

    public DateTimeOffset CreatedAt { get; }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            SetProperty(ref _displayName, value);
        }
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            SetProperty(ref _outputDirectory, value);
        }
    }

    public string? OutputFilePath
    {
        get => _outputFilePath;
        set => SetProperty(ref _outputFilePath, value);
    }

    public int RequestedBitrate
    {
        get => _requestedBitrate;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Bitrate must be greater than zero.");
            }

            SetProperty(ref _requestedBitrate, value);
        }
    }

    public ConversionJobStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, Math.Clamp(value, 0, 100));
    }

    public string? CurrentStage
    {
        get => _currentStage;
        set => SetProperty(ref _currentStage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public DateTimeOffset? StartedAt
    {
        get => _startedAt;
        set => SetProperty(ref _startedAt, value);
    }

    public DateTimeOffset? CompletedAt
    {
        get => _completedAt;
        set => SetProperty(ref _completedAt, value);
    }

    public TimeSpan? Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    public string? ThumbnailUrl
    {
        get => _thumbnailUrl;
        set => SetProperty(ref _thumbnailUrl, value);
    }

    public string? ThumbnailLocalPath
    {
        get => _thumbnailLocalPath;
        set => SetProperty(ref _thumbnailLocalPath, value);
    }

    public MediaMetadata? Metadata
    {
        get => _metadata;
        set => SetProperty(ref _metadata, value);
    }

    public bool EmbedThumbnail
    {
        get => _embedThumbnail;
        set => SetProperty(ref _embedThumbnail, value);
    }

    public int RetryCount
    {
        get => _retryCount;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Retry count cannot be negative.");
            }

            SetProperty(ref _retryCount, value);
        }
    }
}
