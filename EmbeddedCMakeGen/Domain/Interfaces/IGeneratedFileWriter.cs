using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Domain.Interfaces;

public interface IGeneratedFileWriter
{
    void WriteFiles(GeneratedProjectFiles generatedFiles, string outputRootPath, GeneratedFileWriteOptions? options = null);
}
