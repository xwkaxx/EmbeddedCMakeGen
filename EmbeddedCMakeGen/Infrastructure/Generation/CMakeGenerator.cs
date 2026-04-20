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
            new(GccToolchainPath, BuildGccToolchainFile(normalizedModel)),
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
            outputArtifacts: NormalizeValues(projectModel.OutputArtifacts, isPath: false),
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
        var generateBinByDefault = projectModel.OutputArtifacts.Contains("bin", StringComparer.OrdinalIgnoreCase) ? "ON" : "OFF";
        var generateMapByDefault = projectModel.OutputArtifacts.Contains("map", StringComparer.OrdinalIgnoreCase) ? "ON" : "OFF";

        return $$"""
cmake_minimum_required(VERSION 3.22)

project({{EscapeForCMake(projectModel.ProjectName)}} C ASM)

add_executable({{target}})

# USER CODE BEGIN Includes
# target_include_directories({{target}} PRIVATE)
# USER CODE END Includes

add_subdirectory(cmake/stm32cubemx)

option(EMBEDDED_GENERATE_BIN "Generate a .bin file from the ELF output." {{generateBinByDefault}})
option(EMBEDDED_GENERATE_MAP "Generate a linker map file." {{generateMapByDefault}})

if(EMBEDDED_GENERATE_MAP)
    target_link_options({{target}} PRIVATE "-Wl,-Map=$<TARGET_FILE_DIR:{{target}}>/{{target}}.map")
endif()

add_custom_command(TARGET {{target}} POST_BUILD
    COMMAND ${CMAKE_OBJCOPY} -O ihex $<TARGET_FILE:{{target}}> $<TARGET_FILE_DIR:{{target}}>/{{target}}.hex
    COMMENT "Generating HEX artifact"
)

if(EMBEDDED_GENERATE_BIN)
    add_custom_command(TARGET {{target}} POST_BUILD
        COMMAND ${CMAKE_OBJCOPY} -O binary $<TARGET_FILE:{{target}}> $<TARGET_FILE_DIR:{{target}}>/{{target}}.bin
        COMMENT "Generating BIN artifact"
    )
endif()

# USER CODE BEGIN TargetLink
# target_link_libraries({{target}} PRIVATE)
# USER CODE END TargetLink
""";
    }

    private static string BuildPlatformModule(ProjectModel projectModel)
    {
        var appSources = ToPlatformModulePaths(projectModel.ApplicationSources);
        var driverSources = ToPlatformModulePaths(projectModel.DriverSources);
        var middlewareSources = ToPlatformModulePaths(projectModel.MiddlewareSources);
        var startupSources = ToPlatformModulePaths(projectModel.StartupSources);
        var includeDirectories = ToPlatformModulePaths(projectModel.IncludeDirectories);
        var linkDirectories = ToPlatformModulePaths(projectModel.LinkDirectories);
        var architectureFlags = ExtractArchitectureFlags(projectModel.CompileOptions, projectModel.LinkOptions);
        var toolchainDefinitions = ExtractToolchainDefinitions(projectModel.CompileDefinitions);
        var compileDefinitions = projectModel.CompileDefinitions
            .Where(definition => !toolchainDefinitions.Contains(definition, StringComparer.Ordinal))
            .ToArray();
        var compileOptions = projectModel.CompileOptions
            .Where(option => !architectureFlags.Contains(option, StringComparer.Ordinal))
            .ToArray();
        var linkOptions = projectModel.LinkOptions
            .Where(option => !architectureFlags.Contains(option, StringComparer.Ordinal))
            .ToArray();
        var linkerScript = ToPlatformModulePath(projectModel.LinkerScript);

        var target = EscapeForCMake(projectModel.TargetName);
        var builder = new StringBuilder();
        builder.AppendLine("# Auto-generated by EmbeddedCMakeGen. STM32 platform module.");
        builder.AppendLine();

        builder.AppendLine("# Sources");
        AppendList(builder, "EMBEDDED_APP_SOURCES", appSources);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_DRIVER_SOURCES", driverSources);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_MIDDLEWARE_SOURCES", middlewareSources);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_STARTUP_SOURCES", startupSources);
        builder.AppendLine();
        builder.AppendLine("# Includes");
        AppendList(builder, "EMBEDDED_INCLUDE_DIRECTORIES", includeDirectories);
        builder.AppendLine();
        builder.AppendLine("# Compile definitions");
        AppendList(builder, "EMBEDDED_COMPILE_DEFINITIONS", compileDefinitions);
        builder.AppendLine();
        builder.AppendLine("# Compile options");
        AppendList(builder, "EMBEDDED_COMPILE_OPTIONS", compileOptions);
        builder.AppendLine();
        builder.AppendLine("# Link options");
        AppendList(builder, "EMBEDDED_LINK_OPTIONS", linkOptions);
        builder.AppendLine();
        builder.AppendLine("# Linker search paths and libraries");
        AppendList(builder, "EMBEDDED_LINK_DIRECTORIES", linkDirectories);
        builder.AppendLine();
        AppendList(builder, "EMBEDDED_LINKED_LIBRARIES", projectModel.LinkedLibraries);
        builder.AppendLine();
        builder.AppendLine("# Linker script");

        if (linkerScript is null)
        {
            builder.AppendLine("set(EMBEDDED_LINKER_SCRIPT \"\")");
        }
        else
        {
            builder.AppendLine($"set(EMBEDDED_LINKER_SCRIPT \"{EscapeForCMake(linkerScript)}\")");
        }

        builder.AppendLine();
        builder.AppendLine("add_library(stm32cubemx_common INTERFACE)");
        builder.AppendLine("target_include_directories(stm32cubemx_common INTERFACE ${EMBEDDED_INCLUDE_DIRECTORIES})");
        builder.AppendLine("target_compile_definitions(stm32cubemx_common INTERFACE ${EMBEDDED_COMPILE_DEFINITIONS})");
        builder.AppendLine("target_compile_options(stm32cubemx_common INTERFACE ${EMBEDDED_COMPILE_OPTIONS})");
        builder.AppendLine();
        builder.AppendLine("add_library(stm32cubemx_drivers OBJECT)");
        builder.AppendLine("target_sources(stm32cubemx_drivers PRIVATE ${EMBEDDED_DRIVER_SOURCES} ${EMBEDDED_MIDDLEWARE_SOURCES})");
        builder.AppendLine("target_link_libraries(stm32cubemx_drivers PUBLIC stm32cubemx_common)");
        builder.AppendLine("target_include_directories(stm32cubemx_drivers PRIVATE ${EMBEDDED_INCLUDE_DIRECTORIES})");
        builder.AppendLine("target_compile_definitions(stm32cubemx_drivers PRIVATE ${EMBEDDED_COMPILE_DEFINITIONS})");
        builder.AppendLine("target_compile_options(stm32cubemx_drivers PRIVATE ${EMBEDDED_COMPILE_OPTIONS})");
        builder.AppendLine();
        builder.AppendLine($"target_sources({target} PRIVATE ${{EMBEDDED_APP_SOURCES}} ${{EMBEDDED_STARTUP_SOURCES}} $<TARGET_OBJECTS:stm32cubemx_drivers>)");
        builder.AppendLine($"target_link_libraries({target} PRIVATE stm32cubemx_common ${{EMBEDDED_LINKED_LIBRARIES}})");
        builder.AppendLine($"target_include_directories({target} PRIVATE ${{EMBEDDED_INCLUDE_DIRECTORIES}})");
        builder.AppendLine($"target_compile_definitions({target} PRIVATE ${{EMBEDDED_COMPILE_DEFINITIONS}})");
        builder.AppendLine($"target_compile_options({target} PRIVATE ${{EMBEDDED_COMPILE_OPTIONS}})");
        builder.AppendLine($"target_link_directories({target} PRIVATE ${{EMBEDDED_LINK_DIRECTORIES}})");
        builder.AppendLine($"target_link_options({target} PRIVATE ${{EMBEDDED_LINK_OPTIONS}})");
        builder.AppendLine();
        builder.AppendLine("if(EMBEDDED_LINKER_SCRIPT)");
        builder.AppendLine($"    target_link_options({target} PRIVATE \"-T${{EMBEDDED_LINKER_SCRIPT}}\")");
        builder.AppendLine("endif()");
        builder.AppendLine();
        builder.AppendLine($"set_target_properties({target} PROPERTIES SUFFIX \".elf\")");

        return builder.ToString();
    }

    private static IReadOnlyList<string> ToPlatformModulePaths(IReadOnlyList<string> values)
    {
        return values
            .Select(ToPlatformModulePath)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();
    }

    private static string? ToPlatformModulePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().Replace('\\', '/');

        if (normalized.StartsWith("${", StringComparison.Ordinal)
            || normalized.StartsWith("$<", StringComparison.Ordinal)
            || Path.IsPathRooted(normalized))
        {
            return normalized;
        }

        if (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return $"${{CMAKE_CURRENT_SOURCE_DIR}}/../../{normalized}";
    }

    private static string BuildGccToolchainFile(ProjectModel projectModel)
    {
        var architectureFlags = string.Join(" ", ExtractArchitectureFlags(projectModel.CompileOptions, projectModel.LinkOptions));
        var globalDefinitions = string.Join(";", ExtractToolchainDefinitions(projectModel.CompileDefinitions));

        return """
# Auto-generated by EmbeddedCMakeGen.
set(CMAKE_SYSTEM_NAME Generic)
set(CMAKE_SYSTEM_PROCESSOR arm)
set(CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY)

set(TOOLCHAIN_PREFIX arm-none-eabi)
set(TOOLCHAIN_FILE_DIR "${CMAKE_CURRENT_LIST_DIR}")
set(TOOLCHAIN_BIN_PATH "" CACHE PATH "Directory containing GNU Arm Embedded toolchain binaries.")

if(TOOLCHAIN_BIN_PATH)
  if(NOT IS_ABSOLUTE "${TOOLCHAIN_BIN_PATH}")
    set(TOOLCHAIN_BIN_PATH "${TOOLCHAIN_FILE_DIR}/${TOOLCHAIN_BIN_PATH}")
  endif()
  file(TO_CMAKE_PATH "${TOOLCHAIN_BIN_PATH}" TOOLCHAIN_BIN_PATH)
  set(TOOLCHAIN_PREFIX_PATH "${TOOLCHAIN_BIN_PATH}/${TOOLCHAIN_PREFIX}")
else()
  set(TOOLCHAIN_PREFIX_PATH "${TOOLCHAIN_PREFIX}")
endif()

set(CMAKE_C_COMPILER "${TOOLCHAIN_PREFIX_PATH}-gcc")
set(CMAKE_ASM_COMPILER "${TOOLCHAIN_PREFIX_PATH}-gcc")
set(CMAKE_CXX_COMPILER "${TOOLCHAIN_PREFIX_PATH}-g++")
set(CMAKE_LINKER "${TOOLCHAIN_PREFIX_PATH}-g++")
set(CMAKE_AR "${TOOLCHAIN_PREFIX_PATH}-ar")
set(CMAKE_OBJCOPY "${TOOLCHAIN_PREFIX_PATH}-objcopy")
set(CMAKE_SIZE "${TOOLCHAIN_PREFIX_PATH}-size")

set(EMBEDDED_ARCH_FLAGS "@@EMBEDDED_ARCH_FLAGS@@" CACHE STRING "Global architecture and ABI flags for all C/ASM/CXX and linker invocations.")
set(EMBEDDED_DEVICE_DEFINITIONS "@@EMBEDDED_DEVICE_DEFINITIONS@@" CACHE STRING "Global target device preprocessor definitions (semicolon-separated).")

separate_arguments(EMBEDDED_ARCH_FLAGS_LIST NATIVE_COMMAND "${EMBEDDED_ARCH_FLAGS}")
foreach(flag IN LISTS EMBEDDED_ARCH_FLAGS_LIST)
  if(NOT flag STREQUAL "")
    string(APPEND CMAKE_C_FLAGS_INIT " ${flag}")
    string(APPEND CMAKE_CXX_FLAGS_INIT " ${flag}")
    string(APPEND CMAKE_ASM_FLAGS_INIT " ${flag}")
    string(APPEND CMAKE_EXE_LINKER_FLAGS_INIT " ${flag}")
  endif()
endforeach()

foreach(definition IN LISTS EMBEDDED_DEVICE_DEFINITIONS)
  if(NOT definition STREQUAL "")
    string(APPEND CMAKE_C_FLAGS_INIT " -D${definition}")
    string(APPEND CMAKE_CXX_FLAGS_INIT " -D${definition}")
    string(APPEND CMAKE_ASM_FLAGS_INIT " -D${definition}")
  endif()
endforeach()

# Keep find_* lookups focused on the toolchain while still allowing host programs.
set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)
"""
            .Replace("@@EMBEDDED_ARCH_FLAGS@@", EscapeForCMake(architectureFlags))
            .Replace("@@EMBEDDED_DEVICE_DEFINITIONS@@", EscapeForCMake(globalDefinitions));
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
        var architectureFlags = string.Join(" ", ExtractArchitectureFlags(projectModel.CompileOptions, projectModel.LinkOptions));
        var globalDefinitions = string.Join(";", ExtractToolchainDefinitions(projectModel.CompileDefinitions));

        var configureEntries = buildTypes
            .Select(buildType => BuildConfigurePresetEntry(
                buildType,
                generator!,
                toolchainFile!,
                architectureFlags,
                globalDefinitions));

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

    private static string BuildConfigurePresetEntry(
        string buildType,
        string generator,
        string toolchainFile,
        string architectureFlags,
        string globalDefinitions)
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
        "CMAKE_TOOLCHAIN_FILE": "{{EscapeForJson(toolchainFile)}}",
        "EMBEDDED_ARCH_FLAGS": "{{EscapeForJson(architectureFlags)}}",
        "EMBEDDED_DEVICE_DEFINITIONS": "{{EscapeForJson(globalDefinitions)}}"
      }
    }
""";
    }

    private static IReadOnlyList<string> ExtractArchitectureFlags(
        IReadOnlyList<string> compileOptions,
        IReadOnlyList<string> linkOptions)
    {
        return compileOptions
            .Concat(linkOptions)
            .Where(option =>
                option.StartsWith("-mcpu=", StringComparison.Ordinal)
                || option.Equals("-mthumb", StringComparison.Ordinal)
                || option.StartsWith("-mfpu=", StringComparison.Ordinal)
                || option.StartsWith("-mfloat-abi=", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ExtractToolchainDefinitions(IReadOnlyList<string> compileDefinitions)
    {
        return compileDefinitions
            .Where(definition =>
                definition.Equals("USE_HAL_DRIVER", StringComparison.Ordinal)
                || definition.StartsWith("STM32", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
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
