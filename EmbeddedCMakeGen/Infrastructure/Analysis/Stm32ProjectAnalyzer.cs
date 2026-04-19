using System.Text.RegularExpressions;
using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Analysis;

public sealed class Stm32ProjectAnalyzer : IProjectAnalyzer
{
    private static readonly Regex StartupRegex = new(@"(^|/)startup_stm32.*\.(s|S)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SystemRegex = new(@"(^|/)system_stm32.*\.c$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Stm32HeaderRegex = new(@"(^|/)stm32.*\.h$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HalConfRegex = new(@"(^|/)stm32.*_hal_conf\.h$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LinkerFlashRegex = new(@"(^|/)STM32.*_FLASH\.ld$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ChipTokenRegex = new(@"STM32[A-Z0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        var startupSources = ResolveStartupSources(scanResult, userOptions);
        var selectedStartup = startupSources.FirstOrDefault();
        var selectedLinker = ResolveLinkerScript(scanResult, userOptions);

        var chipMacro = userOptions?.ChipMacroOverride
                        ?? InferChipMacro(startupSources, scanResult.HeaderFiles, scanResult.LinkerScripts);
        var useHalDriver = userOptions?.UseHalDriverOverride ?? InferUseHalDriver(scanResult);

        var includeDirectories = ResolveIncludeDirectories(scanResult, userOptions);
        var driverSources = ResolveDriverSources(scanResult);
        var middlewareSources = ResolveMiddlewareSources(scanResult);
        var applicationSources = ResolveApplicationSources(scanResult, startupSources, driverSources, middlewareSources);
        var compileDefinitions = ResolveCompileDefinitions(scanResult, userOptions, chipMacro, useHalDriver);

        return new ProjectModel(
            projectName: projectName,
            targetName: userOptions?.TargetNameOverride ?? projectName,
            platformKind: userOptions?.PreferredPlatform ?? PlatformKind.Stm32,
            sourceFiles: scanResult.CSourceFiles,
            asmFiles: scanResult.AssemblyFiles,
            includeDirectories: includeDirectories,
            linkerScript: selectedLinker,
            compileDefinitions: compileDefinitions,
            compileOptions: ResolveCompileOptions(userOptions),
            linkOptions: ResolveLinkOptions(userOptions),
            toolchainFile: userOptions?.ToolchainFilePath ?? "cmake/gcc-arm-none-eabi.cmake",
            chipMacro: chipMacro,
            useHalDriver: useHalDriver,
            platformFamily: InferPlatformFamily(chipMacro),
            platformSeries: InferPlatformSeries(chipMacro),
            applicationSources: applicationSources,
            driverSources: driverSources,
            middlewareSources: middlewareSources,
            startupSources: startupSources,
            linkDirectories: userOptions?.LinkDirectoriesOverride ?? [],
            linkedLibraries: userOptions?.LinkedLibrariesOverride ?? [],
            toolchainKind: userOptions?.ToolchainKindOverride ?? "gcc-arm-none-eabi",
            supportedBuildTypes: userOptions?.SupportedBuildTypesOverride ?? ["Debug", "Release"],
            presetGenerator: userOptions?.PresetGeneratorOverride ?? "Ninja",
            cmakeModuleStyle: "stm32cubemx",
            platformModuleRelativePath: "cmake/stm32cubemx",
            selectedAnalyzerName: nameof(Stm32ProjectAnalyzer),
            selectedStartupFile: selectedStartup,
            selectedLinkerScript: selectedLinker);
    }

    private static string ResolveProjectName(ScanResult scanResult, UserProjectOptions? userOptions)
    {
        var fallbackName = Path.GetFileName(scanResult.RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return userOptions?.ProjectNameOverride ?? fallbackName;
    }

    private static IReadOnlyList<string> ResolveStartupSources(ScanResult scanResult, UserProjectOptions? userOptions)
    {
        if (!string.IsNullOrWhiteSpace(userOptions?.StartupFilePath))
        {
            return [Normalize(userOptions.StartupFilePath)];
        }

        var startupCandidates = scanResult.AssemblyFiles
            .Where(path => StartupRegex.IsMatch(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (startupCandidates.Length > 0)
        {
            return [startupCandidates[0]];
        }

        return scanResult.AssemblyFiles
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string? ResolveLinkerScript(ScanResult scanResult, UserProjectOptions? userOptions)
    {
        if (!string.IsNullOrWhiteSpace(userOptions?.LinkerScriptPath))
        {
            return Normalize(userOptions.LinkerScriptPath);
        }

        var preferred = scanResult.LinkerScripts
            .Where(path => LinkerFlashRegex.IsMatch(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault();

        return preferred
               ?? scanResult.LinkerScripts
                   .OrderBy(path => path, StringComparer.Ordinal)
                   .FirstOrDefault();
    }

    private static IReadOnlyList<string> ResolveIncludeDirectories(ScanResult scanResult, UserProjectOptions? userOptions)
    {
        if (userOptions?.IncludeDirectoriesOverride is { Count: > 0 })
        {
            return userOptions.IncludeDirectoriesOverride
                .Select(Normalize)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        var includeDirectories = new List<string>();

        AddIfExists(scanResult, includeDirectories, "Core/Inc");
        AddFirstMatchingPath(scanResult, includeDirectories, path => path.Contains("/STM32", StringComparison.OrdinalIgnoreCase)
                                                                && path.Contains("HAL_Driver/Inc", StringComparison.OrdinalIgnoreCase));
        AddFirstMatchingPath(scanResult, includeDirectories, path => path.Contains("HAL_Driver/Inc/Legacy", StringComparison.OrdinalIgnoreCase));
        AddFirstMatchingPath(scanResult, includeDirectories, path => path.Contains("Drivers/CMSIS/Device/ST", StringComparison.OrdinalIgnoreCase)
                                                                && path.EndsWith("/Include", StringComparison.OrdinalIgnoreCase));
        AddIfExists(scanResult, includeDirectories, "Drivers/CMSIS/Include");

        includeDirectories.AddRange(scanResult.Directories.Where(IsApplicationIncludeDirectory));

        return includeDirectories
            .Select(Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ResolveApplicationSources(
        ScanResult scanResult,
        IReadOnlyList<string> startupSources,
        IReadOnlyList<string> driverSources,
        IReadOnlyList<string> middlewareSources)
    {
        var excluded = new HashSet<string>(driverSources.Concat(middlewareSources), StringComparer.Ordinal);
        var appDirMarkers = new[] { "/app/", "/application/", "/bldc/", "/kernel/", "/motor/" };

        var result = scanResult.CSourceFiles
            .Where(path => !excluded.Contains(path))
            .Where(path => path.StartsWith("Core/Src/", StringComparison.OrdinalIgnoreCase)
                           || appDirMarkers.Any(marker => path.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            .Concat(startupSources)
            .Select(Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (result.Length > 0)
        {
            return result;
        }

        return scanResult.CSourceFiles
            .Where(path => !excluded.Contains(path))
            .Select(Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ResolveDriverSources(ScanResult scanResult)
    {
        return scanResult.CSourceFiles
            .Where(path => SystemRegex.IsMatch(path)
                           || path.Contains("/STM32", StringComparison.OrdinalIgnoreCase)
                           && path.Contains("HAL_Driver/Src/", StringComparison.OrdinalIgnoreCase))
            .Select(Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ResolveMiddlewareSources(ScanResult scanResult)
    {
        return scanResult.CSourceFiles
            .Where(path => path.StartsWith("Middlewares/", StringComparison.OrdinalIgnoreCase)
                           || path.Contains("/Middlewares/", StringComparison.OrdinalIgnoreCase))
            .Select(Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private IReadOnlyList<string> ResolveCompileDefinitions(
        ScanResult scanResult,
        UserProjectOptions? userOptions,
        string? chipMacro,
        bool useHalDriver)
    {
        if (userOptions?.CompileDefinitionsOverride is { Count: > 0 })
        {
            return userOptions.CompileDefinitionsOverride
                .Select(Normalize)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        var definitions = new List<string>();

        if (useHalDriver)
        {
            definitions.Add("USE_HAL_DRIVER");
        }

        if (!string.IsNullOrWhiteSpace(chipMacro))
        {
            definitions.Add(chipMacro);
        }

        if (scanResult.RootPath.Contains("debug", StringComparison.OrdinalIgnoreCase))
        {
            definitions.Add("DEBUG");
        }

        return definitions
            .Select(Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
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

    private static bool InferUseHalDriver(ScanResult scanResult)
    {
        return ContainsHalDriverPath(scanResult)
               || scanResult.HeaderFiles.Any(path => HalConfRegex.IsMatch(path));
    }

    private static string? InferChipMacro(
        IReadOnlyList<string> startupSources,
        IReadOnlyList<string> headerFiles,
        IReadOnlyList<string> linkerScripts)
    {
        var candidates = startupSources.Concat(headerFiles).Concat(linkerScripts)
            .Select(TryExtractChipMacro)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return candidates
            .Select(value => value!)
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.Key.ToUpperInvariant())
            .FirstOrDefault();
    }

    private static string? TryExtractChipMacro(string path)
    {
        var match = ChipTokenRegex.Match(path);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static string? InferPlatformFamily(string? chipMacro)
    {
        if (string.IsNullOrWhiteSpace(chipMacro) || chipMacro!.Length < 7)
        {
            return null;
        }

        return chipMacro[..7];
    }

    private static string? InferPlatformSeries(string? chipMacro)
    {
        var family = InferPlatformFamily(chipMacro);
        if (family is null || family.Length < 7)
        {
            return null;
        }

        return family[5..7];
    }

    private static void AddIfExists(ScanResult scanResult, ICollection<string> includes, string path)
    {
        var normalized = Normalize(path);
        if (scanResult.Directories.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            includes.Add(normalized);
        }
    }

    private static void AddFirstMatchingPath(ScanResult scanResult, ICollection<string> includes, Func<string, bool> predicate)
    {
        var match = scanResult.Directories
            .Where(predicate)
            .OrderBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(match))
        {
            includes.Add(match);
        }
    }

    private static bool IsApplicationIncludeDirectory(string path)
    {
        return path.EndsWith("/Inc", StringComparison.OrdinalIgnoreCase)
               && (path.StartsWith("Core/", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/App", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/Application", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/BLDC", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/Kernel", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/Motor", StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string path)
    {
        return path.Replace('\\', '/').Trim();
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

        static bool IsHalPath(string path, string markerValue, string suffixValue)
        {
            var normalized = path.Replace('\\', '/');
            var markerIndex = normalized.IndexOf(markerValue, StringComparison.OrdinalIgnoreCase);
            return markerIndex >= 0
                   && normalized.IndexOf(suffixValue, markerIndex, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        return scanResult.Directories.Any(path => IsHalPath(path, marker, suffix))
               || scanResult.CSourceFiles.Any(path => IsHalPath(path, marker, suffix))
               || scanResult.HeaderFiles.Any(path => IsHalPath(path, marker, suffix));
    }
}
