using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;
using EmbeddedCMakeGen.Infrastructure.Analysis;
using EmbeddedCMakeGen.Infrastructure.Generation;
using EmbeddedCMakeGen.Infrastructure.IO;
using EmbeddedCMakeGen.Infrastructure.Logging;
using EmbeddedCMakeGen.Infrastructure.Scanning;

namespace EmbeddedCMakeGen.Commands;

public sealed class CommandDispatcher
{
    public int Dispatch(string[] args)
    {
        var logger = new ConsoleLogger();

        try
        {
            if (IsHelpRequest(args))
            {
                PrintHelp(logger);
                return 0;
            }

            var command = ParsedCommand.Parse(args);
            if (!command.IsValid)
            {
                logger.Info(command.ValidationMessage ?? "Invalid command.");
                PrintHelp(logger);
                return 1;
            }

            var rootPath = Path.GetFullPath(command.RootPath!);
            if (!Directory.Exists(rootPath))
            {
                logger.Error($"Root directory does not exist: {rootPath}");
                return 1;
            }

            IProjectScanner scanner = new ProjectScanner();
            var scanResult = scanner.Scan(rootPath);

            if (command.CommandName == CommandName.Scan)
            {
                PrintScanSummary(logger, scanResult);
                return 0;
            }

            var selector = new AnalyzerSelector([new Stm32ProjectAnalyzer(), new GenericEmbeddedCAnalyzer()]);
            var analyzer = selector.SelectBestAnalyzer(scanResult);
            var userOptions = BuildUserOptions(command);
            var projectModel = analyzer.Analyze(scanResult, userOptions);

            ICMakeGenerator generator = new CMakeGenerator();
            var generatedFiles = generator.Generate(projectModel);

            IGeneratedFileWriter writer = new GeneratedFileWriter(logger, new FileBackupService(), new AtomicFileReplaceService());
            var outputRoot = Path.GetFullPath(command.OutputPath ?? rootPath);

            logger.Info($"Analyzer: {projectModel.SelectedAnalyzerName ?? analyzer.GetType().Name}");
            logger.Info($"Mode: {(command.CommandName == CommandName.Preview ? "preview" : "generate")}");
            logger.Info($"Selected startup: {projectModel.SelectedStartupFile ?? "(none)"}");
            logger.Info($"Selected linker: {projectModel.SelectedLinkerScript ?? "(none)"}");
            logger.Info($"Items app/driver/middleware/include: {projectModel.ApplicationSources.Count}/{projectModel.DriverSources.Count}/{projectModel.MiddlewareSources.Count}/{projectModel.IncludeDirectories.Count}");

            writer.WriteFiles(
                generatedFiles,
                outputRoot,
                new GeneratedFileWriteOptions(
                    previewOnly: command.CommandName == CommandName.Preview,
                    allowOverwrite: true,
                    createBackupBeforeOverwrite: command.CreateBackup));

            return 0;
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
            return 1;
        }
    }

    private static bool IsHelpRequest(string[] args)
    {
        return args.Length == 1 &&
               (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("help", StringComparison.OrdinalIgnoreCase));
    }

    private static void PrintHelp(ILogger logger)
    {
        logger.Info("EmbeddedCMakeGen V1");
        logger.Info("STM32 CMake Environment Generator");
        logger.Info(string.Empty);
        logger.Info(ParsedCommand.UsageText);
        logger.Info(string.Empty);
        logger.Info("Examples:");
        logger.Info("  EmbeddedCMakeGen scan --root .");
        logger.Info("  EmbeddedCMakeGen preview --root . --out ./regen");
        logger.Info("  EmbeddedCMakeGen generate --root . --out . --backup");
    }

    private static UserProjectOptions BuildUserOptions(ParsedCommand command)
    {
        return new UserProjectOptions
        {
            PreferredPlatform = ParsePlatform(command.Platform),
            ProjectNameOverride = command.ProjectName,
            TargetNameOverride = command.TargetName,
            LinkerScriptPath = command.Linker,
            StartupFilePath = command.Startup,
            ChipMacroOverride = command.Chip
        };
    }

    private static PlatformKind? ParsePlatform(string? platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            return null;
        }

        return platform.ToLowerInvariant() switch
        {
            "stm32" => PlatformKind.Stm32,
            "generic" => PlatformKind.GenericEmbeddedC,
            "generic-embedded-c" => PlatformKind.GenericEmbeddedC,
            _ => null
        };
    }

    private static void PrintScanSummary(ILogger logger, ScanResult scanResult)
    {
        logger.Info("Scan summary:");
        logger.Info($"  Root: {scanResult.RootPath}");
        logger.Info($"  .c files: {scanResult.CSourceFiles.Count}");
        logger.Info($"  .h files: {scanResult.HeaderFiles.Count}");
        logger.Info($"  .s/.S files: {scanResult.AssemblyFiles.Count}");
        logger.Info($"  .ld files: {scanResult.LinkerScripts.Count}");
        logger.Info($"  directories discovered: {scanResult.Directories.Count}");
    }

    private enum CommandName
    {
        Scan,
        Preview,
        Generate
    }

    private sealed record ParsedCommand(
        bool IsValid,
        string? ValidationMessage,
        CommandName CommandName,
        string? RootPath,
        string? OutputPath,
        string? ProjectName,
        string? TargetName,
        string? Platform,
        string? Chip,
        string? Startup,
        string? Linker,
        bool CreateBackup)
    {
        public const string UsageText = "Usage: EmbeddedCMakeGen <scan|preview|generate> --root <projectRoot> [--out <outputDir>] [--project-name <name>] [--target-name <name>] [--platform <stm32|generic>] [--chip <chipId>] [--startup <path>] [--linker <path>] [--backup] [--help]";

        public static ParsedCommand Parse(string[] args)
        {
            if (args.Length == 0)
            {
                return Invalid("Missing command.");
            }

            var commandToken = args[0].Trim().ToLowerInvariant();
            var commandName = commandToken switch
            {
                "scan" => CommandName.Scan,
                "preview" => CommandName.Preview,
                "generate" => CommandName.Generate,
                _ => (CommandName?)null
            };

            if (!commandName.HasValue)
            {
                return Invalid($"Unknown command: {args[0]}");
            }

            string? root = null;
            string? output = null;
            string? projectName = null;
            string? targetName = null;
            string? platform = null;
            string? chip = null;
            string? startup = null;
            string? linker = null;
            var createBackup = false;

            for (var i = 1; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "--root" when i + 1 < args.Length:
                        root = args[++i];
                        break;
                    case "--out" when i + 1 < args.Length:
                    case "--output" when i + 1 < args.Length:
                        output = args[++i];
                        break;
                    case "--project-name" when i + 1 < args.Length:
                        projectName = args[++i];
                        break;
                    case "--target-name" when i + 1 < args.Length:
                        targetName = args[++i];
                        break;
                    case "--platform" when i + 1 < args.Length:
                        platform = args[++i];
                        break;
                    case "--chip" when i + 1 < args.Length:
                        chip = args[++i];
                        break;
                    case "--startup" when i + 1 < args.Length:
                        startup = args[++i];
                        break;
                    case "--linker" when i + 1 < args.Length:
                        linker = args[++i];
                        break;
                    case "--backup":
                        createBackup = true;
                        break;
                    default:
                        return Invalid($"Unknown or incomplete argument: {arg}");
                }
            }

            if (string.IsNullOrWhiteSpace(root))
            {
                return Invalid("Missing required argument: --root");
            }

            return new ParsedCommand(
                IsValid: true,
                ValidationMessage: null,
                CommandName: commandName.Value,
                RootPath: root,
                OutputPath: output,
                ProjectName: projectName,
                TargetName: targetName,
                Platform: platform,
                Chip: chip,
                Startup: startup,
                Linker: linker,
                CreateBackup: createBackup);
        }

        private static ParsedCommand Invalid(string reason)
        {
            return new ParsedCommand(
                IsValid: false,
                ValidationMessage: reason,
                CommandName: CommandName.Preview,
                RootPath: null,
                OutputPath: null,
                ProjectName: null,
                TargetName: null,
                Platform: null,
                Chip: null,
                Startup: null,
                Linker: null,
                CreateBackup: false);
        }
    }
}
