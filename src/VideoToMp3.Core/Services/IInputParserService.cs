using VideoToMp3.Core.Inputs;

namespace VideoToMp3.Core.Services;

public interface IInputParserService
{
    InputParseResult Parse(string? input);
}
