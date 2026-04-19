using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Scanning;

public sealed class ProjectScanner : IProjectScanner
{
    public ScanResult Scan(string rootPath)
    {
        return new ScanResult(rootPath);
    }
}
