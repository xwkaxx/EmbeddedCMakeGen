namespace EmbeddedCMakeGen.Domain.Models;

public sealed class GeneratedFile
{
    public GeneratedFile(string relativePath, string content)
    {
        RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public string RelativePath { get; }

    public string Content { get; }
}
