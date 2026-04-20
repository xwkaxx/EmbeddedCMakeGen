namespace EmbeddedCMakeGen.Domain.Models;

public sealed class UserProjectOptions
{
    public PlatformKind? PreferredPlatform { get; init; }

    public string? ProjectNameOverride { get; init; }

    public string? TargetNameOverride { get; init; }

    public string? ToolchainFilePath { get; init; }

    public string? ToolchainKindOverride { get; init; }

    public string? LinkerScriptPath { get; init; }

    public string? StartupFilePath { get; init; }

    public string? ChipMacroOverride { get; init; }

    public bool? UseHalDriverOverride { get; init; }

    public IReadOnlyList<string>? IncludeDirectoriesOverride { get; init; }

    public IReadOnlyList<string>? CompileDefinitionsOverride { get; init; }

    public IReadOnlyList<string>? CompileOptionsOverride { get; init; }

    public IReadOnlyList<string>? LinkOptionsOverride { get; init; }

    public IReadOnlyList<string>? LinkDirectoriesOverride { get; init; }

    public IReadOnlyList<string>? LinkedLibrariesOverride { get; init; }

    public bool? GenerateBinArtifactOverride { get; init; }

    public bool? GenerateMapArtifactOverride { get; init; }

    public IReadOnlyList<string>? SupportedBuildTypesOverride { get; init; }

    public string? PresetGeneratorOverride { get; init; }

    public bool IncludeCommonStm32Definitions { get; init; }

    public string CStandard { get; init; } = "c11";

    public bool TreatWarningsAsErrors { get; init; }
}
