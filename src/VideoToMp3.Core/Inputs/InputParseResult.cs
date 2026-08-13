namespace VideoToMp3.Core.Inputs;

public sealed record InputParseResult(
    IReadOnlyList<ParsedInput> Items,
    IReadOnlyList<InputParseError> Errors,
    int DuplicateCount)
{
    public bool IsValid => Errors.Count == 0;
}
