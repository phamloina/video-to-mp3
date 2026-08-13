using VideoToMp3.Core.Models;

namespace VideoToMp3.Core.Inputs;

public sealed record ParsedInput(
    ConversionSourceType SourceType,
    string Source);
