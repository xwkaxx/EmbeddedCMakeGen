namespace EmbeddedCMakeGen.Domain.Models;

public sealed class GeneratedProjectFiles
{
    public static GeneratedProjectFiles Empty { get; } = new([]);

    public GeneratedProjectFiles(IReadOnlyList<GeneratedFile> files)
    {
        Files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public IReadOnlyList<GeneratedFile> Files { get; }
}
