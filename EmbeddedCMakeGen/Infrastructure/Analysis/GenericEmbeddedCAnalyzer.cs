using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Analysis;

public sealed class GenericEmbeddedCAnalyzer : IProjectAnalyzer
{
    public AnalysisMatchResult Match(ScanResult scanResult)
    {
        return new AnalysisMatchResult(PlatformKind.GenericEmbeddedC, confidence: 1, reason: "Generic fallback analyzer match.");
    }

    public ProjectModel Analyze(ScanResult scanResult, UserProjectOptions? userOptions = null)
    {
        return new ProjectModel(
            projectName: Path.GetFileName(scanResult.RootPath),
            platform: userOptions?.PreferredPlatform ?? PlatformKind.GenericEmbeddedC,
            cSourceFiles: scanResult.CSourceFiles,
            headerFiles: scanResult.HeaderFiles,
            assemblyFiles: scanResult.AssemblyFiles,
            linkerScript: scanResult.LinkerScripts.FirstOrDefault(),
            userOptions: userOptions);
    }
}
