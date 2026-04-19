using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.IO;

public sealed class GeneratedFileWriter : IGeneratedFileWriter
{
    private readonly ILogger _logger;
    private readonly FileBackupService _fileBackupService;
    private readonly AtomicFileReplaceService _atomicFileReplaceService;

    public GeneratedFileWriter(
        ILogger logger,
        FileBackupService fileBackupService,
        AtomicFileReplaceService atomicFileReplaceService)
    {
        _logger = logger;
        _fileBackupService = fileBackupService;
        _atomicFileReplaceService = atomicFileReplaceService;
    }

    public void WriteFiles(GeneratedProjectFiles generatedFiles, string outputRootPath, GeneratedFileWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(generatedFiles);

        var resolvedOptions = options ?? new GeneratedFileWriteOptions();
        var normalizedOutputRoot = Path.GetFullPath(outputRootPath);

        foreach (var generatedFile in generatedFiles.Files)
        {
            var normalizedRelativePath = generatedFile.RelativePath.Replace('\\', '/').TrimStart('/');
            var targetFullPath = Path.GetFullPath(Path.Combine(normalizedOutputRoot, normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            var fileExists = File.Exists(targetFullPath);
            var action = fileExists ? "overwrite" : "create";

            if (resolvedOptions.PreviewOnly)
            {
                _logger.Info($"[preview] {action}: {normalizedRelativePath}");
                continue;
            }

            if (fileExists && !resolvedOptions.AllowOverwrite)
            {
                _logger.Info($"[skip] exists: {normalizedRelativePath}");
                continue;
            }

            if (fileExists && resolvedOptions.CreateBackupBeforeOverwrite)
            {
                var backupPath = _fileBackupService.CreateBackup(targetFullPath);
                _logger.Info($"[backup] {normalizedRelativePath} -> {backupPath}");
            }

            _atomicFileReplaceService.Replace(targetFullPath, generatedFile.Content);
            _logger.Info($"[write] {action}: {normalizedRelativePath}");
        }
    }
}
