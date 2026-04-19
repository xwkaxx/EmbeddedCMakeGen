namespace EmbeddedCMakeGen.Domain.Models;

public sealed class ProjectModel
{
    public ProjectModel(
        string projectName,
        string targetName,
        PlatformKind platformKind,
        IReadOnlyList<string>? sourceFiles = null,
        IReadOnlyList<string>? asmFiles = null,
        IReadOnlyList<string>? includeDirectories = null,
        string? linkerScript = null,
        IReadOnlyList<string>? compileDefinitions = null,
        IReadOnlyList<string>? compileOptions = null,
        IReadOnlyList<string>? linkOptions = null,
        string? toolchainFile = null)
    {
        ProjectName = projectName;
        TargetName = targetName;
        PlatformKind = platformKind;
        SourceFiles = sourceFiles ?? [];
        AsmFiles = asmFiles ?? [];
        IncludeDirectories = includeDirectories ?? [];
        LinkerScript = linkerScript;
        CompileDefinitions = compileDefinitions ?? [];
        CompileOptions = compileOptions ?? [];
        LinkOptions = linkOptions ?? [];
        ToolchainFile = toolchainFile;
    }

    public string ProjectName { get; }

    public string TargetName { get; }

    public PlatformKind PlatformKind { get; }

    public IReadOnlyList<string> SourceFiles { get; }

    public IReadOnlyList<string> AsmFiles { get; }

    public IReadOnlyList<string> IncludeDirectories { get; }

    public string? LinkerScript { get; }

    public IReadOnlyList<string> CompileDefinitions { get; }

    public IReadOnlyList<string> CompileOptions { get; }

    public IReadOnlyList<string> LinkOptions { get; }

    public string? ToolchainFile { get; }
}
