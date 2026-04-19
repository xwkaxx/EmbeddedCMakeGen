namespace EmbeddedCMakeGen.Domain.Models;

public sealed class AnalysisMatchResult
{
    public AnalysisMatchResult(PlatformKind platform, int confidence, string reason)
    {
        if (confidence is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 100.");
        }

        Platform = platform;
        Confidence = confidence;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    public PlatformKind Platform { get; }

    public int Confidence { get; }

    public string Reason { get; }
}
