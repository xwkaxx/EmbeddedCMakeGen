namespace EmbeddedCMakeGen.Domain.Models;

public sealed class UserProjectOptions
{
    public PlatformKind? PreferredPlatform { get; init; }

    public string? ProjectNameOverride { get; init; }

    public string? TargetNameOverride { get; init; }

    public string? ToolchainFilePath { get; init; }

    public string? LinkerScriptPath { get; init; }

    public IReadOnlyList<string>? IncludeDirectoriesOverride { get; init; }

    public IReadOnlyList<string>? CompileDefinitionsOverride { get; init; }

    public IReadOnlyList<string>? CompileOptionsOverride { get; init; }

    public IReadOnlyList<string>? LinkOptionsOverride { get; init; }

    public bool IncludeCommonStm32Definitions { get; init; }

    public string CStandard { get; init; } = "c11";

    public bool TreatWarningsAsErrors { get; init; }
}
