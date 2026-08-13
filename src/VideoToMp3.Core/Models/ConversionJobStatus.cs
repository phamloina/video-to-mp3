namespace VideoToMp3.Core.Models;

public enum ConversionJobStatus
{
    Waiting,
    Analyzing,
    Downloading,
    Converting,
    Expanded,
    Completed,
    Failed,
    Canceled
}
