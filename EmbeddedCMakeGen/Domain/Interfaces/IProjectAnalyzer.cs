using EmbeddedCMakeGen.Application.Models;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Domain.Interfaces;

public interface IProjectAnalyzer
{
    ProjectModel Analyze(ScanResult scanResult);
}
