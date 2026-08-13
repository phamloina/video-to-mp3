namespace VideoToMp3.Core.Media;

public sealed record AudioConversionResult(
    bool IsSuccess,
    string? OutputFilePath,
    string? ErrorMessage,
    string? TechnicalDetails)
{
    public static AudioConversionResult Success(string outputFilePath) =>
        new(true, outputFilePath, null, null);

    public static AudioConversionResult Failure(
        string errorMessage,
        string? technicalDetails = null) =>
        new(false, null, errorMessage, technicalDetails);
}
