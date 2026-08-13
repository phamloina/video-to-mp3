namespace VideoToMp3.Core.Inputs;

public sealed record InputParseError(
    string Input,
    string Message);
