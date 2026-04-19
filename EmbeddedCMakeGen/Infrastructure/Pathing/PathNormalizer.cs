namespace EmbeddedCMakeGen.Infrastructure.Pathing;

public sealed class PathNormalizer
{
    public string NormalizeRoot(string rootPath)
    {
        return Path.GetFullPath(rootPath);
    }

    public string ToRelativeForwardSlashPath(string rootPath, string fullPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, fullPath);
        return relativePath.Replace('\\', '/');
    }
}
