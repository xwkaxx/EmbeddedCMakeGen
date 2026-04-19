using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Domain.Interfaces;

public interface IProjectScanner
{
    ScanResult Scan(string rootPath);
}
