using System.Text;
using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Generation;

public sealed class CMakeGenerator : ICMakeGenerator
{
    private const string MainCMakeListsPath = "CMakeLists.txt";
    private const string PresetsPath = "CMakePresets.json";
    private const string SourcesPath = "cmake/generated_sources.cmake";
    private const string PlatformPath = "cmake/generated_platform.cmake";

    public GeneratedProjectFiles Generate(ProjectModel projectModel)
    {
        ArgumentNullException.ThrowIfNull(projectModel);

        var normalizedModel = Normalize(projectModel);

        var files = new List<GeneratedFile>
        {
            new(MainCMakeListsPath, BuildCMakeLists(normalizedModel)),
            new(PresetsPath, BuildCMakePresets(normalizedModel)),
            new(SourcesPath, BuildGeneratedSources(normalizedModel)),
            new(PlatformPath, BuildGeneratedPlatform(normalizedModel))
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
            toolchainFile: NormalizeSingle(projectModel.ToolchainFile, isPath: true));
    }

    private static IReadOnlyList<string> NormalizeValues(IReadOnlyList<string> values, bool isPath)
    {
        return values
            .Select(value => NormalizeSingle(value, isPath))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray()!;
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
        var builder = new StringBuilder();
        builder.AppendLine("cmake_minimum_required(VERSION 3.22)");
        builder.AppendLine();
        builder.AppendLine($"project({EscapeForCMake(projectModel.ProjectName)} C ASM)");
        builder.AppendLine();
        builder.AppendLine("include(cmake/generated_sources.cmake)");
        builder.AppendLine("include(cmake/generated_platform.cmake)");
        builder.AppendLine();
        builder.AppendLine($"add_executable({EscapeForCMake(projectModel.TargetName)})");
        builder.AppendLine();
        builder.AppendLine($"target_sources({EscapeForCMake(projectModel.TargetName)} PRIVATE");
        builder.AppendLine("    ${GENERATED_SOURCE_FILES}");
        builder.AppendLine("    ${GENERATED_ASM_FILES}");
        builder.AppendLine(")");
        builder.AppendLine();
        builder.AppendLine($"target_include_directories({EscapeForCMake(projectModel.TargetName)} PRIVATE");
        builder.AppendLine("    ${GENERATED_INCLUDE_DIRECTORIES}");
        builder.AppendLine(")");
        builder.AppendLine();
        builder.AppendLine($"target_compile_definitions({EscapeForCMake(projectModel.TargetName)} PRIVATE");
        builder.AppendLine("    ${GENERATED_COMPILE_DEFINITIONS}");
        builder.AppendLine(")");
        builder.AppendLine();
        builder.AppendLine($"target_compile_options({EscapeForCMake(projectModel.TargetName)} PRIVATE");
        builder.AppendLine("    ${GENERATED_COMPILE_OPTIONS}");
        builder.AppendLine(")");
        builder.AppendLine();
        builder.AppendLine($"target_link_options({EscapeForCMake(projectModel.TargetName)} PRIVATE");
        builder.AppendLine("    ${GENERATED_LINK_OPTIONS}");
        builder.AppendLine(")");
        builder.AppendLine();
        builder.AppendLine("if(GENERATED_LINKER_SCRIPT)");
        builder.AppendLine($"    target_link_options({EscapeForCMake(projectModel.TargetName)} PRIVATE");
        builder.AppendLine("        \"-T${GENERATED_LINKER_SCRIPT}\"");
        builder.AppendLine("    )");
        builder.AppendLine("endif()");

        return builder.ToString();
    }

    private static string BuildGeneratedSources(ProjectModel projectModel)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Auto-generated by EmbeddedCMakeGen. Do not edit manually.");
        builder.AppendLine();

        AppendList(builder, "GENERATED_SOURCE_FILES", projectModel.SourceFiles);
        builder.AppendLine();
        AppendList(builder, "GENERATED_ASM_FILES", projectModel.AsmFiles);
        builder.AppendLine();
        AppendList(builder, "GENERATED_INCLUDE_DIRECTORIES", projectModel.IncludeDirectories);
        builder.AppendLine();

        if (projectModel.LinkerScript is null)
        {
            builder.AppendLine("set(GENERATED_LINKER_SCRIPT \"\")");
        }
        else
        {
            builder.AppendLine($"set(GENERATED_LINKER_SCRIPT \"{EscapeForCMake(projectModel.LinkerScript)}\")");
        }

        return builder.ToString();
    }

    private static string BuildGeneratedPlatform(ProjectModel projectModel)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Auto-generated by EmbeddedCMakeGen. Do not edit manually.");
        builder.AppendLine();

        AppendList(builder, "GENERATED_COMPILE_DEFINITIONS", projectModel.CompileDefinitions);
        builder.AppendLine();
        AppendList(builder, "GENERATED_COMPILE_OPTIONS", projectModel.CompileOptions);
        builder.AppendLine();
        AppendList(builder, "GENERATED_LINK_OPTIONS", projectModel.LinkOptions);
        builder.AppendLine();

        if (projectModel.ToolchainFile is null)
        {
            builder.AppendLine("set(GENERATED_TOOLCHAIN_FILE_HINT \"\")");
        }
        else
        {
            builder.AppendLine($"set(GENERATED_TOOLCHAIN_FILE_HINT \"{EscapeForCMake(projectModel.ToolchainFile)}\")");
        }

        return builder.ToString();
    }

    private static string BuildCMakePresets(ProjectModel projectModel)
    {
        var toolchainClause = projectModel.ToolchainFile is null
            ? string.Empty
            : $",\n        \"CMAKE_TOOLCHAIN_FILE\": \"{EscapeForJson(projectModel.ToolchainFile)}\"";

        return $$"""
{
  "version": 6,
  "configurePresets": [
    {
      "name": "debug",
      "displayName": "Debug",
      "generator": "Ninja",
      "binaryDir": "${sourceDir}/build/debug",
      "cacheVariables": {
        "CMAKE_BUILD_TYPE": "Debug"{{toolchainClause}}
      }
    },
    {
      "name": "release",
      "displayName": "Release",
      "generator": "Ninja",
      "binaryDir": "${sourceDir}/build/release",
      "cacheVariables": {
        "CMAKE_BUILD_TYPE": "Release"{{toolchainClause}}
      }
    }
  ],
  "buildPresets": [
    {
      "name": "debug",
      "configurePreset": "debug"
    },
    {
      "name": "release",
      "configurePreset": "release"
    }
  ]
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
