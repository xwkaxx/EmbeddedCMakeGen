using System.Text;
using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Generation;

public sealed class CMakeGenerator : ICMakeGenerator
{
    private const string MainCMakeListsPath = "CMakeLists.txt";
    private const string PresetsPath = "CMakePresets.json";
    private const string GccToolchainPath = "cmake/gcc-arm-none-eabi.cmake";
    private const string ClangToolchainPath = "cmake/starm-clang.cmake";
    private const string PlatformModulePath = "cmake/stm32cubemx/CMakeLists.txt";

    public GeneratedProjectFiles Generate(ProjectModel projectModel)
    {
        ArgumentNullException.ThrowIfNull(projectModel);

        var normalizedModel = Normalize(projectModel);

        var files = new List<GeneratedFile>
        {
            new(MainCMakeListsPath, BuildCMakeLists(normalizedModel)),
            new(PresetsPath, BuildCMakePresets(normalizedModel)),
            new(GccToolchainPath, BuildGccToolchainFile()),
            new(ClangToolchainPath, BuildClangToolchainFile()),
            new(PlatformModulePath, BuildPlatformModule(normalizedModel))
        };

        return new GeneratedProjectFiles(files);
    }

    private static ProjectModel Normalize(ProjectModel projectModel)
    {
        return new ProjectModel(
            projectName: projectModel.ProjectName,
            targetName: projectModel.TargetName,
            platformKind: projectModel.PlatformKind,
            sourceFiles: NormalizeValues(projectModel.SourceFiles, isPath: true),
            asmFiles: NormalizeValues(projectModel.AsmFiles, isPath: true),
            includeDirectories: NormalizeValues(projectModel.IncludeDirectories, isPath: true),
            linkerScript: NormalizeSingle(projectModel.LinkerScript, isPath: true),
            compileDefinitions: NormalizeValues(projectModel.CompileDefinitions, isPath: false),
            compileOptions: NormalizeValues(projectModel.CompileOptions, isPath: false),
            linkOptions: NormalizeValues(projectModel.LinkOptions, isPath: false),
            toolchainFile: NormalizeSingle(projectModel.ToolchainFile, isPath: true),
            chipMacro: NormalizeSingle(projectModel.ChipMacro, isPath: false),
            useHalDriver: projectModel.UseHalDriver,
            platformFamily: NormalizeSingle(projectModel.PlatformFamily, isPath: false),
            platformSeries: NormalizeSingle(projectModel.PlatformSeries, isPath: false),
            applicationSources: NormalizeValues(projectModel.ApplicationSources, isPath: true),
            driverSources: NormalizeValues(projectModel.DriverSources, isPath: true),
            middlewareSources: NormalizeValues(projectModel.MiddlewareSources, isPath: true),
            startupSources: NormalizeValues(projectModel.StartupSources, isPath: true),
            linkDirectories: NormalizeValues(projectModel.LinkDirectories, isPath: true),
            linkedLibraries: NormalizeValues(projectModel.LinkedLibraries, isPath: false),
            toolchainKind: NormalizeSingle(projectModel.ToolchainKind, isPath: false),
            supportedBuildTypes: NormalizeValues(projectModel.SupportedBuildTypes, isPath: false),
            presetGenerator: NormalizeSingle(projectModel.PresetGenerator, isPath: false),
            cmakeModuleStyle: NormalizeSingle(projectModel.CMakeModuleStyle, isPath: false),
            platformModuleRelativePath: NormalizeSingle(projectModel.PlatformModuleRelativePath, isPath: true),
            selectedAnalyzerName: NormalizeSingle(projectModel.SelectedAnalyzerName, isPath: false),
            selectedStartupFile: NormalizeSingle(projectModel.SelectedStartupFile, isPath: true),
            selectedLinkerScript: NormalizeSingle(projectModel.SelectedLinkerScript, isPath: true));
    }

    private static IReadOnlyList<string> NormalizeValues(IReadOnlyList<string> values, bool isPath)
    {
        return values
            .Select(value => NormalizeSingle(value, isPath))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();
    }

    private static string? NormalizeSingle(string? value, bool isPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (isPath)
        {
            normalized = normalized.Replace('\\', '/');
        }

        return normalized;
    }

    private static string BuildCMakeLists(ProjectModel projectModel)
    {
        var target = EscapeForCMake(projectModel.TargetName);

        return $$"""
cmake_minimum_required(VERSION 3.22)

project({{EscapeForCMake(projectModel.ProjectName)}} C ASM)

add_executable({{target}})

# USER CODE BEGIN Includes
# target_include_directories({{target}} PRIVATE)
# USER CODE END Includes

add_subdirectory(cmake/stm32cubemx)

# USER CODE BEGIN TargetLink
# target_link_libraries({{target}} PRIVATE)
# USER CODE END TargetLink
""";
    }

    private static string BuildPlatformModule(ProjectModel projectModel)
    {
        var target = EscapeForCMake(projectModel.TargetName);
        var builder = new StringBuilder();
        builder.AppendLine("# Auto-generated by EmbeddedCMakeGen. STM32 platform module.");
        builder.AppendLine();

        AppendList(builder, "EMBEDDED_APP_SOURCES", projectModel.ApplicationSources);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_DRIVER_SOURCES", projectModel.DriverSources);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_MIDDLEWARE_SOURCES", projectModel.MiddlewareSources);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_STARTUP_SOURCES", projectModel.StartupSources);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_INCLUDE_DIRECTORIES", projectModel.IncludeDirectories);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_COMPILE_DEFINITIONS", projectModel.CompileDefinitions);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_COMPILE_OPTIONS", projectModel.CompileOptions);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_LINK_OPTIONS", projectModel.LinkOptions);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_LINK_DIRECTORIES", projectModel.LinkDirectories);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_LINKED_LIBRARIES", projectModel.LinkedLibraries);
        builder.AppendLine();

        if (projectModel.LinkerScript is null)
        {
            builder.AppendLine("set(EMBEDDED_LINKER_SCRIPT \"\")");
        }
        else
        {
            builder.AppendLine($"set(EMBEDDED_LINKER_SCRIPT \"{EscapeForCMake(projectModel.LinkerScript)}\")");
        }

        builder.AppendLine();
        builder.AppendLine("add_library(stm32cubemx_common INTERFACE)");
        builder.AppendLine("target_include_directories(stm32cubemx_common INTERFACE ${EMBEDDED_INCLUDE_DIRECTORIES})");
        builder.AppendLine("target_compile_definitions(stm32cubemx_common INTERFACE ${EMBEDDED_COMPILE_DEFINITIONS})");
        builder.AppendLine();
        builder.AppendLine("add_library(stm32cubemx_drivers OBJECT)");
        builder.AppendLine("target_sources(stm32cubemx_drivers PRIVATE ${EMBEDDED_DRIVER_SOURCES} ${EMBEDDED_MIDDLEWARE_SOURCES})");
        builder.AppendLine("target_link_libraries(stm32cubemx_drivers PUBLIC stm32cubemx_common)");
        builder.AppendLine("target_compile_options(stm32cubemx_drivers PRIVATE ${EMBEDDED_COMPILE_OPTIONS})");
        builder.AppendLine();
        builder.AppendLine($"target_sources({target} PRIVATE ${EMBEDDED_APP_SOURCES} ${EMBEDDED_STARTUP_SOURCES} $<TARGET_OBJECTS:stm32cubemx_drivers>)");
        builder.AppendLine($"target_link_libraries({target} PRIVATE stm32cubemx_common ${EMBEDDED_LINKED_LIBRARIES})");
        builder.AppendLine($"target_compile_options({target} PRIVATE ${EMBEDDED_COMPILE_OPTIONS})");
        builder.AppendLine($"target_link_directories({target} PRIVATE ${EMBEDDED_LINK_DIRECTORIES})");
        builder.AppendLine($"target_link_options({target} PRIVATE ${EMBEDDED_LINK_OPTIONS})");
        builder.AppendLine();
        builder.AppendLine("if(EMBEDDED_LINKER_SCRIPT)");
        builder.AppendLine($"    target_link_options({target} PRIVATE \"-T${EMBEDDED_LINKER_SCRIPT}\")");
        builder.AppendLine("endif()");

        return builder.ToString();
    }

    private static string BuildGccToolchainFile()
    {
        return """
# Auto-generated by EmbeddedCMakeGen.
set(CMAKE_SYSTEM_NAME Generic)
set(CMAKE_SYSTEM_PROCESSOR arm)

set(CMAKE_C_COMPILER arm-none-eabi-gcc)
set(CMAKE_ASM_COMPILER arm-none-eabi-gcc)
set(CMAKE_AR arm-none-eabi-ar)
set(CMAKE_OBJCOPY arm-none-eabi-objcopy)
set(CMAKE_SIZE arm-none-eabi-size)

set(CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY)
""";
    }

    private static string BuildClangToolchainFile()
    {
        return """
# Auto-generated by EmbeddedCMakeGen.
set(CMAKE_SYSTEM_NAME Generic)
set(CMAKE_SYSTEM_PROCESSOR arm)

set(CMAKE_C_COMPILER clang)
set(CMAKE_ASM_COMPILER clang)
set(CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY)
""";
    }

    private static string BuildCMakePresets(ProjectModel projectModel)
    {
        var buildTypes = projectModel.SupportedBuildTypes.Count > 0
            ? projectModel.SupportedBuildTypes
            : ["Debug", "Release"];

        var generator = string.IsNullOrWhiteSpace(projectModel.PresetGenerator)
            ? "Ninja"
            : projectModel.PresetGenerator;

        var toolchainFile = string.IsNullOrWhiteSpace(projectModel.ToolchainFile)
            ? "cmake/gcc-arm-none-eabi.cmake"
            : projectModel.ToolchainFile;

        var configureEntries = buildTypes
            .Select(buildType => BuildConfigurePresetEntry(buildType, generator!, toolchainFile!));

        var buildEntries = buildTypes
            .Select(buildType => BuildBuildPresetEntry(buildType));

        return $$"""
{
  "version": 6,
  "configurePresets": [
{{string.Join(",\n", configureEntries)}}
  ],
  "buildPresets": [
{{string.Join(",\n", buildEntries)}}
  ]
}
""";
    }

    private static string BuildConfigurePresetEntry(string buildType, string generator, string toolchainFile)
    {
        var key = buildType.ToLowerInvariant();
        return $$"""
    {
      "name": "{{EscapeForJson(key)}}",
      "displayName": "{{EscapeForJson(buildType)}}",
      "generator": "{{EscapeForJson(generator)}}",
      "binaryDir": "${sourceDir}/build/{{EscapeForJson(key)}}",
      "cacheVariables": {
        "CMAKE_BUILD_TYPE": "{{EscapeForJson(buildType)}}",
        "CMAKE_TOOLCHAIN_FILE": "{{EscapeForJson(toolchainFile)}}"
      }
    }
""";
    }

    private static string BuildBuildPresetEntry(string buildType)
    {
        var key = buildType.ToLowerInvariant();
        return $$"""
    {
      "name": "{{EscapeForJson(key)}}",
      "configurePreset": "{{EscapeForJson(key)}}"
    }
""";
    }

    private static void AppendList(StringBuilder builder, string variableName, IReadOnlyList<string> values)
    {
        builder.AppendLine($"set({variableName}");

        foreach (var value in values)
        {
            builder.AppendLine($"    \"{EscapeForCMake(value)}\"");
        }

        builder.AppendLine(")");
    }

    private static string EscapeForCMake(string input)
    {
        return input
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string EscapeForJson(string input)
    {
        return input
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
