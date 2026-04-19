using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Analysis;

public sealed class GenericEmbeddedCAnalyzer : IProjectAnalyzer
{
    public int Priority => 1;

    public AnalysisMatchResult Match(ScanResult scanResult)
    {
        var hasC = scanResult.CSourceFiles.Count > 0;
        var hasHeaders = scanResult.HeaderFiles.Count > 0;
        var hasAsm = scanResult.AssemblyFiles.Count > 0;
        var hasLd = scanResult.LinkerScripts.Count > 0;

        var score = (hasC, hasHeaders) switch
        {
            (true, true) => 60,
            (true, false) => 45,
            (false, true) => 20,
            _ => 0
        };

        if (hasAsm)
        {
            score += 5;
        }

        if (hasLd)
        {
            score += 5;
        }

        score = Math.Min(score, 75);

        var reason = score > 0
            ? "Generic embedded C file set detected."
            : "No generic embedded C scan signals detected.";

        return new AnalysisMatchResult(PlatformKind.GenericEmbeddedC, score, reason);
    }

    public ProjectModel Analyze(ScanResult scanResult, UserProjectOptions? userOptions = null)
    {
        var projectName = ResolveProjectName(scanResult, userOptions);
        var startupSources = ResolveStartupSources(scanResult, userOptions);
        var linkerScript = userOptions?.LinkerScriptPath ?? scanResult.LinkerScripts.FirstOrDefault();

        return new ProjectModel(
            projectName: projectName,
            targetName: userOptions?.TargetNameOverride ?? projectName,
            platformKind: userOptions?.PreferredPlatform ?? PlatformKind.GenericEmbeddedC,
            sourceFiles: scanResult.CSourceFiles,
            asmFiles: scanResult.AssemblyFiles,
            includeDirectories: ResolveIncludeDirectories(scanResult, userOptions),
            linkerScript: linkerScript,
            compileDefinitions: userOptions?.CompileDefinitionsOverride ?? [],
            compileOptions: ResolveCompileOptions(userOptions),
            linkOptions: userOptions?.LinkOptionsOverride ?? [],
            toolchainFile: userOptions?.ToolchainFilePath,
            applicationSources: scanResult.CSourceFiles,
            startupSources: startupSources,
            selectedAnalyzerName: nameof(GenericEmbeddedCAnalyzer),
            selectedStartupFile: startupSources.FirstOrDefault(),
            selectedLinkerScript: linkerScript,
            toolchainKind: userOptions?.ToolchainKindOverride,
            supportedBuildTypes: userOptions?.SupportedBuildTypesOverride ?? ["Debug", "Release"],
            presetGenerator: userOptions?.PresetGeneratorOverride ?? "Ninja",
            linkDirectories: userOptions?.LinkDirectoriesOverride ?? [],
            linkedLibraries: userOptions?.LinkedLibrariesOverride ?? []);
    }

    private static IReadOnlyList<string> ResolveStartupSources(ScanResult scanResult, UserProjectOptions? userOptions)
    {
        if (!string.IsNullOrWhiteSpace(userOptions?.StartupFilePath))
        {
            return [userOptions.StartupFilePath.Replace('\\', '/')];
        }

        return scanResult.AssemblyFiles;
    }

    private static string ResolveProjectName(ScanResult scanResult, UserProjectOptions? userOptions)
    {
        var fallbackName = Path.GetFileName(scanResult.RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return userOptions?.ProjectNameOverride ?? fallbackName;
    }

    private static IReadOnlyList<string> ResolveIncludeDirectories(ScanResult scanResult, UserProjectOptions? userOptions)
    {
        if (userOptions?.IncludeDirectoriesOverride is { Count: > 0 })
        {
            return userOptions.IncludeDirectoriesOverride;
        }

        return scanResult.Directories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ResolveCompileOptions(UserProjectOptions? userOptions)
    {
        if (userOptions?.CompileOptionsOverride is { Count: > 0 })
        {
            return userOptions.CompileOptionsOverride;
        }

        return userOptions?.TreatWarningsAsErrors == true ? ["-Werror"] : [];
    }
}
