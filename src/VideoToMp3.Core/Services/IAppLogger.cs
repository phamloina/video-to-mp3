namespace VideoToMp3.Core.Services;

public interface IAppLogger
{
    void LogError(Guid jobId, string userMessage, string? technicalDetails = null);
}
