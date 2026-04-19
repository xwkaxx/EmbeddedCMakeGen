using EmbeddedCMakeGen.Application.Models;
using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Analysis;

public sealed class Stm32ProjectAnalyzer : IProjectAnalyzer
{
    public ProjectModel Analyze(ScanResult scanResult)
    {
        return new ProjectModel();
    }
}
