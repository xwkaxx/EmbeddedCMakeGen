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
            var command = ParsedCommand.Parse(args);
            if (!command.IsValid)
            {
                logger.Info(ParsedCommand.UsageText);
                return 1;
            }

            IProjectScanner scanner = new ProjectScanner();
            var scanResult = scanner.Scan(command.RootPath!);

            var selector = new AnalyzerSelector([new Stm32ProjectAnalyzer(), new GenericEmbeddedCAnalyzer()]);
            var analyzer = selector.SelectBestAnalyzer(scanResult);
            var projectModel = analyzer.Analyze(scanResult);

            ICMakeGenerator generator = new CMakeGenerator();
            var generatedFiles = generator.Generate(projectModel);

            IGeneratedFileWriter writer = new GeneratedFileWriter(logger, new FileBackupService(), new AtomicFileReplaceService());
            var outputRoot = command.OutputPath ?? command.RootPath!;

            writer.WriteFiles(
                generatedFiles,
                outputRoot,
                new GeneratedFileWriteOptions(
                    previewOnly: command.PreviewOnly,
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

    private sealed record ParsedCommand(bool IsValid, string? RootPath, string? OutputPath, bool PreviewOnly, bool CreateBackup)
    {
        public const string UsageText = "Usage: EmbeddedCMakeGen --root <projectRoot> [--output <outputDir>] [--generate] [--backup]";

        public static ParsedCommand Parse(string[] args)
        {
            if (args.Length == 0)
            {
                return new ParsedCommand(false, null, null, previewOnly: true, createBackup: false);
            }

            string? root = null;
            string? output = null;
            var previewOnly = true;
            var createBackup = false;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "--root" when i + 1 < args.Length:
                        root = args[++i];
                        break;
                    case "--output" when i + 1 < args.Length:
                        output = args[++i];
                        break;
                    case "--generate":
                        previewOnly = false;
                        break;
                    case "--preview":
                        previewOnly = true;
                        break;
                    case "--backup":
                        createBackup = true;
                        break;
                    default:
                        return new ParsedCommand(false, null, null, previewOnly: true, createBackup: false);
                }
            }

            if (string.IsNullOrWhiteSpace(root))
            {
                return new ParsedCommand(false, null, null, previewOnly: true, createBackup: false);
            }

            return new ParsedCommand(true, root, output, previewOnly, createBackup);
        }
    }
}
