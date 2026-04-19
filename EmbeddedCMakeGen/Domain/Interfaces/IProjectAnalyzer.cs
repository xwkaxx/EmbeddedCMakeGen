using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Domain.Interfaces;

public interface IProjectAnalyzer
{
    AnalysisMatchResult Match(ScanResult scanResult);

    ProjectModel Analyze(ScanResult scanResult, UserProjectOptions? userOptions = null);
}
