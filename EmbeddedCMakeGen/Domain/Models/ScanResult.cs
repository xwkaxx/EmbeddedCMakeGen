namespace EmbeddedCMakeGen.Domain.Models;

public sealed class ScanResult
{
    public ScanResult(
        string rootPath,
        IReadOnlyList<string>? cSourceFiles = null,
        IReadOnlyList<string>? headerFiles = null,
        IReadOnlyList<string>? assemblyFiles = null,
        IReadOnlyList<string>? linkerScripts = null)
    {
        RootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
        CSourceFiles = cSourceFiles ?? [];
        HeaderFiles = headerFiles ?? [];
        AssemblyFiles = assemblyFiles ?? [];
        LinkerScripts = linkerScripts ?? [];
    }

    public string RootPath { get; }

    public IReadOnlyList<string> CSourceFiles { get; }

    public IReadOnlyList<string> HeaderFiles { get; }

    public IReadOnlyList<string> AssemblyFiles { get; }

    public IReadOnlyList<string> LinkerScripts { get; }
}
