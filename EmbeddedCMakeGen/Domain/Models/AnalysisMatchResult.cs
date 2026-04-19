namespace EmbeddedCMakeGen.Domain.Models;

public sealed class AnalysisMatchResult
{
    public AnalysisMatchResult(PlatformKind platform, int confidence, string reason)
    {
        Platform = platform;
        Confidence = confidence;
        Reason = reason;
    }

    public PlatformKind Platform { get; }

    public int Confidence { get; }

    public string Reason { get; }
}
