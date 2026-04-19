namespace EmbeddedCMakeGen.Domain.Models;

public sealed class GeneratedFileWriteOptions
{
    public GeneratedFileWriteOptions(bool previewOnly = false, bool allowOverwrite = true, bool createBackupBeforeOverwrite = false)
    {
        PreviewOnly = previewOnly;
        AllowOverwrite = allowOverwrite;
        CreateBackupBeforeOverwrite = createBackupBeforeOverwrite;
    }

    public bool PreviewOnly { get; }

    public bool AllowOverwrite { get; }

    public bool CreateBackupBeforeOverwrite { get; }
}
