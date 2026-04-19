namespace EmbeddedCMakeGen.Domain.Models;

public sealed class UserProjectOptions
{
    public PlatformKind? PreferredPlatform { get; init; }

    public string? ToolchainFilePath { get; init; }

    public string CStandard { get; init; } = "c11";

    public bool TreatWarningsAsErrors { get; init; }
}
