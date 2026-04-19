namespace EmbeddedCMakeGen.Domain.Models;

public sealed class ProjectModel
{
    public ProjectModel(
        string projectName,
        PlatformKind platform,
        IReadOnlyList<string>? cSourceFiles = null,
        IReadOnlyList<string>? headerFiles = null,
        IReadOnlyList<string>? assemblyFiles = null,
        string? linkerScript = null,
        UserProjectOptions? userOptions = null)
    {
        ProjectName = projectName;
        Platform = platform;
        CSourceFiles = cSourceFiles ?? [];
        HeaderFiles = headerFiles ?? [];
        AssemblyFiles = assemblyFiles ?? [];
        LinkerScript = linkerScript;
        UserOptions = userOptions ?? new UserProjectOptions();
    }

    public string ProjectName { get; }

    public PlatformKind Platform { get; }

    public IReadOnlyList<string> CSourceFiles { get; }

    public IReadOnlyList<string> HeaderFiles { get; }

    public IReadOnlyList<string> AssemblyFiles { get; }

    public string? LinkerScript { get; }

    public UserProjectOptions UserOptions { get; }
}
