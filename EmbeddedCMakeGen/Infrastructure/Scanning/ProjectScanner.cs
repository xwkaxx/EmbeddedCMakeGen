using EmbeddedCMakeGen.Application.Models;
using EmbeddedCMakeGen.Domain.Interfaces;

namespace EmbeddedCMakeGen.Infrastructure.Scanning;

public sealed class ProjectScanner : IProjectScanner
{
    public ScanResult Scan(string rootPath)
    {
        return new ScanResult();
    }
}
