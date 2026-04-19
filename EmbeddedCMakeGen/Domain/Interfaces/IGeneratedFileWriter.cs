using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Domain.Interfaces;

public interface IGeneratedFileWriter
{
    void WriteFiles(GeneratedProjectFiles generatedFiles, string outputRootPath, bool previewOnly = false);
}
