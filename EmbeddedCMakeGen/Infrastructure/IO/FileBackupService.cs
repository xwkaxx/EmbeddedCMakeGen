namespace EmbeddedCMakeGen.Infrastructure.IO;

public sealed class FileBackupService
{
    public string CreateBackup(string filePath)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var backupPath = $"{filePath}.{timestamp}.bak";
        File.Copy(filePath, backupPath, overwrite: false);
        return backupPath;
    }
}
