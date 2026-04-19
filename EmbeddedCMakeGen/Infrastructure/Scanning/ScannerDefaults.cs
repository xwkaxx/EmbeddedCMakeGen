namespace EmbeddedCMakeGen.Infrastructure.Scanning;

internal static class ScannerDefaults
{
    public static readonly IReadOnlySet<string> IgnoredDirectoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".vs",
        ".idea",
        "build",
        "Debug",
        "Release",
        "bin",
        "obj",
        "x64",
        "out"
    };

    public const string CMakeBuildPrefix = "cmake-build-";
}
