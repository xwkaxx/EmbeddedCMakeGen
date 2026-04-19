using System.Text.RegularExpressions;
using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Analysis;

public sealed class Stm32ProjectAnalyzer : IProjectAnalyzer
{
    private static readonly Regex StartupRegex = new(@"startup_stm32.*\.s$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SystemRegex = new(@"system_stm32.*\.c$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Stm32HeaderRegex = new(@"(^|/)stm32.*\.h$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HalConfRegex = new(@"stm32.*_hal_conf\.h$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public int Priority => 100;

    public AnalysisMatchResult Match(ScanResult scanResult)
    {
        var score = 0;
        var reasons = new List<string>();

        if (ContainsPath(scanResult, "drivers/cmsis"))
        {
            score += 30;
            reasons.Add("Drivers/CMSIS path detected");
        }

        if (ContainsHalDriverPath(scanResult))
        {
            score += 30;
            reasons.Add("STM32 HAL driver path detected");
        }

        if (scanResult.AssemblyFiles.Any(path => StartupRegex.IsMatch(path)))
        {
            score += 20;
            reasons.Add("startup_stm32*.s file detected");
        }

        if (scanResult.CSourceFiles.Any(path => SystemRegex.IsMatch(path)))
        {
            score += 15;
            reasons.Add("system_stm32*.c file detected");
        }

        if (scanResult.HeaderFiles.Any(path => Stm32HeaderRegex.IsMatch(path)))
        {
            score += 15;
            reasons.Add("stm32*.h header detected");
        }

        if (scanResult.HeaderFiles.Any(path => HalConfRegex.IsMatch(path)))
        {
            score += 20;
            reasons.Add("stm32*_hal_conf.h header detected");
        }

        score = Math.Min(score, 100);
        var reason = reasons.Count > 0
            ? string.Join("; ", reasons)
            : "No STM32-specific scan signals detected.";

        return new AnalysisMatchResult(PlatformKind.Stm32, confidence: score, reason: reason);
    }

    public ProjectModel Analyze(ScanResult scanResult, UserProjectOptions? userOptions = null)
    {
        var projectName = ResolveProjectName(scanResult, userOptions);
        var includeDirectories = ResolveIncludeDirectories(scanResult, userOptions);
        var compileDefinitions = ResolveCompileDefinitions(scanResult, userOptions);

        return new ProjectModel(
            projectName: projectName,
            targetName: userOptions?.TargetNameOverride ?? projectName,
            platformKind: userOptions?.PreferredPlatform ?? PlatformKind.Stm32,
            sourceFiles: scanResult.CSourceFiles,
            asmFiles: scanResult.AssemblyFiles,
            includeDirectories: includeDirectories,
            linkerScript: userOptions?.LinkerScriptPath ?? scanResult.LinkerScripts.FirstOrDefault(),
            compileDefinitions: compileDefinitions,
            compileOptions: ResolveCompileOptions(userOptions),
            linkOptions: ResolveLinkOptions(userOptions),
            toolchainFile: userOptions?.ToolchainFilePath);
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

    private IReadOnlyList<string> ResolveCompileDefinitions(ScanResult scanResult, UserProjectOptions? userOptions)
    {
        if (userOptions?.CompileDefinitionsOverride is { Count: > 0 })
        {
            return userOptions.CompileDefinitionsOverride;
        }

        if (userOptions?.IncludeCommonStm32Definitions != true)
        {
            return [];
        }

        var match = Match(scanResult);
        var includeHalDefinition = match.Confidence >= 40 && scanResult.HeaderFiles.Any(path => HalConfRegex.IsMatch(path));

        return includeHalDefinition ? ["USE_HAL_DRIVER"] : [];
    }

    private static IReadOnlyList<string> ResolveCompileOptions(UserProjectOptions? userOptions)
    {
        if (userOptions?.CompileOptionsOverride is { Count: > 0 })
        {
            return userOptions.CompileOptionsOverride;
        }

        var options = new List<string>();
        if (userOptions?.TreatWarningsAsErrors == true)
        {
            options.Add("-Werror");
        }

        return options;
    }

    private static IReadOnlyList<string> ResolveLinkOptions(UserProjectOptions? userOptions)
    {
        if (userOptions?.LinkOptionsOverride is { Count: > 0 })
        {
            return userOptions.LinkOptionsOverride;
        }

        return [];
    }

    private static bool ContainsPath(ScanResult scanResult, string pathPart)
    {
        return scanResult.Directories.Any(path => path.Contains(pathPart, StringComparison.OrdinalIgnoreCase))
               || scanResult.CSourceFiles.Any(path => path.Contains(pathPart, StringComparison.OrdinalIgnoreCase))
               || scanResult.HeaderFiles.Any(path => path.Contains(pathPart, StringComparison.OrdinalIgnoreCase))
               || scanResult.AssemblyFiles.Any(path => path.Contains(pathPart, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsHalDriverPath(ScanResult scanResult)
    {
        const string marker = "drivers/stm32";
        const string suffix = "hal_driver";

        bool IsHalPath(string path)
        {
            var normalized = path.Replace('\\', '/');
            var markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return markerIndex >= 0
                   && normalized.IndexOf(suffix, markerIndex, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        return scanResult.Directories.Any(IsHalPath)
               || scanResult.CSourceFiles.Any(IsHalPath)
               || scanResult.HeaderFiles.Any(IsHalPath);
    }
}
