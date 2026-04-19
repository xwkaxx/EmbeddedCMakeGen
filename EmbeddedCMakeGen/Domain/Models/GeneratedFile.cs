namespace EmbeddedCMakeGen.Domain.Models;

public sealed class GeneratedFile
{
    public GeneratedFile(string relativePath, string content)
    {
        RelativePath = relativePath;
        Content = content;
    }

    public string RelativePath { get; }

    public string Content { get; }
}
