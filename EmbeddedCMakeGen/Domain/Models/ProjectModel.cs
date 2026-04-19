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
        string? toolchainFile = null,
        string? chipMacro = null,
        bool? useHalDriver = null,
        string? platformFamily = null,
        string? platformSeries = null,
        IReadOnlyList<string>? applicationSources = null,
        IReadOnlyList<string>? driverSources = null,
        IReadOnlyList<string>? middlewareSources = null,
        IReadOnlyList<string>? startupSources = null,
        IReadOnlyList<string>? linkDirectories = null,
        IReadOnlyList<string>? linkedLibraries = null,
        string? toolchainKind = null,
        IReadOnlyList<string>? supportedBuildTypes = null,
        string? presetGenerator = null,
        string? cmakeModuleStyle = null,
        string? platformModuleRelativePath = null,
        string? selectedAnalyzerName = null,
        string? selectedStartupFile = null,
        string? selectedLinkerScript = null)
    {
        ProjectName = projectName;
        TargetName = targetName;
        PlatformKind = platformKind;

        ApplicationSources = applicationSources ?? sourceFiles ?? [];
        DriverSources = driverSources ?? [];
        MiddlewareSources = middlewareSources ?? [];
        StartupSources = startupSources ?? asmFiles ?? [];

        SourceFiles = sourceFiles ?? BuildSourceFiles(ApplicationSources, DriverSources, MiddlewareSources);
        AsmFiles = asmFiles ?? StartupSources;

        IncludeDirectories = includeDirectories ?? [];
        LinkDirectories = linkDirectories ?? [];
        LinkerScript = linkerScript;
        CompileDefinitions = compileDefinitions ?? [];
        CompileOptions = compileOptions ?? [];
        LinkOptions = linkOptions ?? [];
        LinkedLibraries = linkedLibraries ?? [];

        ToolchainFile = toolchainFile;
        ToolchainKind = toolchainKind;
        SupportedBuildTypes = supportedBuildTypes ?? ["Debug", "Release"];
        PresetGenerator = presetGenerator ?? "Ninja";

        ChipMacro = chipMacro;
        UseHalDriver = useHalDriver;
        PlatformFamily = platformFamily;
        PlatformSeries = platformSeries;
        CMakeModuleStyle = cmakeModuleStyle ?? "stm32cubemx";
        PlatformModuleRelativePath = platformModuleRelativePath ?? "cmake/stm32cubemx";

        SelectedAnalyzerName = selectedAnalyzerName;
        SelectedStartupFile = selectedStartupFile ?? StartupSources.FirstOrDefault();
        SelectedLinkerScript = selectedLinkerScript ?? linkerScript;
    }

    public string ProjectName { get; }

    public string TargetName { get; }

    public PlatformKind PlatformKind { get; }

    public string? ChipMacro { get; }

    public bool? UseHalDriver { get; }

    public string? PlatformFamily { get; }

    public string? PlatformSeries { get; }

    public IReadOnlyList<string> SourceFiles { get; }

    public IReadOnlyList<string> AsmFiles { get; }

    public IReadOnlyList<string> ApplicationSources { get; }

    public IReadOnlyList<string> DriverSources { get; }

    public IReadOnlyList<string> MiddlewareSources { get; }

    public IReadOnlyList<string> StartupSources { get; }

    public IReadOnlyList<string> IncludeDirectories { get; }

    public IReadOnlyList<string> LinkDirectories { get; }

    public string? LinkerScript { get; }

    public IReadOnlyList<string> CompileDefinitions { get; }

    public IReadOnlyList<string> CompileOptions { get; }

    public IReadOnlyList<string> LinkOptions { get; }

    public IReadOnlyList<string> LinkedLibraries { get; }

    public string? ToolchainFile { get; }

    public string? ToolchainKind { get; }

    public IReadOnlyList<string> SupportedBuildTypes { get; }

    public string? PresetGenerator { get; }

    public string? CMakeModuleStyle { get; }

    public string? PlatformModuleRelativePath { get; }

    public string? SelectedAnalyzerName { get; }

    public string? SelectedStartupFile { get; }

    public string? SelectedLinkerScript { get; }

    private static IReadOnlyList<string> BuildSourceFiles(params IReadOnlyList<string>[] sourceGroups)
    {
        return sourceGroups
            .SelectMany(group => group)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }
}
