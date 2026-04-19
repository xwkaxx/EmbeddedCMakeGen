using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Analysis;

public sealed class GenericEmbeddedCAnalyzer : IProjectAnalyzer
{
    public AnalysisMatchResult Match(ScanResult scanResult)
    {
        return new AnalysisMatchResult(
            platform: PlatformKind.GenericEmbeddedC,
            confidence: 0,
            reason: "Platform matching is not implemented yet.");
    }

    public ProjectModel Analyze(ScanResult scanResult, UserProjectOptions? userOptions = null)
    {
        return new ProjectModel(
            projectName: "unnamed-project",
            platform: userOptions?.PreferredPlatform ?? PlatformKind.GenericEmbeddedC,
            userOptions: userOptions);
    }
}
