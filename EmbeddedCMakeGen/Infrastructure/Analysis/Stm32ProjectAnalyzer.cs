using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Analysis;

public sealed class Stm32ProjectAnalyzer : IProjectAnalyzer
{
    public AnalysisMatchResult Match(ScanResult scanResult)
    {
        return new AnalysisMatchResult(PlatformKind.Stm32, confidence: 50, reason: "Stub STM32 analyzer match.");
    }

    public ProjectModel Analyze(ScanResult scanResult, UserProjectOptions? userOptions = null)
    {
        return new ProjectModel(
            projectName: Path.GetFileName(scanResult.RootPath),
            platform: userOptions?.PreferredPlatform ?? PlatformKind.Stm32,
            cSourceFiles: scanResult.CSourceFiles,
            headerFiles: scanResult.HeaderFiles,
            assemblyFiles: scanResult.AssemblyFiles,
            linkerScript: scanResult.LinkerScripts.FirstOrDefault(),
            userOptions: userOptions);
    }
}
