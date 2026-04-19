using System.Text;

namespace EmbeddedCMakeGen.Infrastructure.IO;

public sealed class AtomicFileReplaceService
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public void Replace(string targetFilePath, string content)
    {
        var directory = Path.GetDirectoryName(targetFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempFilePath = $"{targetFilePath}.tmp.{Guid.NewGuid():N}";

        try
        {
            File.WriteAllText(tempFilePath, content, Utf8NoBom);

            if (File.Exists(targetFilePath))
            {
                File.Move(tempFilePath, targetFilePath, overwrite: true);
            }
            else
            {
                File.Move(tempFilePath, targetFilePath);
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
