namespace VideoToMp3.Core.Models;

public enum ConversionJobStatus
{
    Waiting,
    Analyzing,
    Downloading,
    Converting,
    Completed,
    Failed,
    Canceled
}
