using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Domain.Interfaces;

public interface IProjectAnalyzer
{
    int Priority { get; }

    AnalysisMatchResult Match(ScanResult scanResult);

    ProjectModel Analyze(ScanResult scanResult, UserProjectOptions? userOptions = null);
}
