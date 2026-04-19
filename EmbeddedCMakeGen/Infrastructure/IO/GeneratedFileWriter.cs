using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.IO;

public sealed class GeneratedFileWriter : IGeneratedFileWriter
{
    private readonly ILogger _logger;

    public GeneratedFileWriter(ILogger logger)
    {
        _logger = logger;
    }

    public void WriteFiles(GeneratedProjectFiles generatedFiles, string outputRootPath, bool previewOnly = false)
    {
        if (previewOnly)
        {
            _logger.Info($"Preview mode: {generatedFiles.Files.Count} files would be written to '{outputRootPath}'.");
            return;
        }

        _logger.Info($"Write requested for {generatedFiles.Files.Count} files to '{outputRootPath}' (not implemented yet).");
    }
}
