using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;
using EmbeddedCMakeGen.Infrastructure.Pathing;

namespace EmbeddedCMakeGen.Infrastructure.Scanning;

public sealed class ProjectScanner : IProjectScanner
{
    private readonly PathNormalizer _pathNormalizer = new();

    public ScanResult Scan(string rootPath)
    {
        var normalizedRootPath = _pathNormalizer.NormalizeRoot(rootPath);
        if (!Directory.Exists(normalizedRootPath))
        {
            throw new DirectoryNotFoundException($"Scan root directory was not found: {normalizedRootPath}");
        }

        var directories = new HashSet<string>(StringComparer.Ordinal);
        var cSourceFiles = new HashSet<string>(StringComparer.Ordinal);
        var headerFiles = new HashSet<string>(StringComparer.Ordinal);
        var assemblyFiles = new HashSet<string>(StringComparer.Ordinal);
        var linkerScripts = new HashSet<string>(StringComparer.Ordinal);

        ScanDirectory(normalizedRootPath, normalizedRootPath, directories, cSourceFiles, headerFiles, assemblyFiles, linkerScripts);

        return new ScanResult(
            normalizedRootPath,
            directories: directories.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
            cSourceFiles: cSourceFiles.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
            headerFiles: headerFiles.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
            assemblyFiles: assemblyFiles.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
            linkerScripts: linkerScripts.OrderBy(path => path, StringComparer.Ordinal).ToArray());
    }

    private void ScanDirectory(
        string rootPath,
        string currentDirectory,
        ISet<string> directories,
        ISet<string> cSourceFiles,
        ISet<string> headerFiles,
        ISet<string> assemblyFiles,
        ISet<string> linkerScripts)
    {
        foreach (var childDirectory in Directory.EnumerateDirectories(currentDirectory))
        {
            var directoryName = Path.GetFileName(childDirectory);
            if (ShouldIgnoreDirectory(directoryName))
            {
                continue;
            }

            ScanDirectory(rootPath, childDirectory, directories, cSourceFiles, headerFiles, assemblyFiles, linkerScripts);
        }

        foreach (var filePath in Directory.EnumerateFiles(currentDirectory))
        {
            var extension = Path.GetExtension(filePath);
            var relativeFilePath = _pathNormalizer.ToRelativeForwardSlashPath(rootPath, filePath);

            switch (extension)
            {
                case ".c":
                    cSourceFiles.Add(relativeFilePath);
                    directories.Add(GetRelativeDirectoryPath(rootPath, filePath));
                    break;
                case ".h":
                    headerFiles.Add(relativeFilePath);
                    directories.Add(GetRelativeDirectoryPath(rootPath, filePath));
                    break;
                case ".s":
                case ".S":
                    assemblyFiles.Add(relativeFilePath);
                    directories.Add(GetRelativeDirectoryPath(rootPath, filePath));
                    break;
                case ".ld":
                    linkerScripts.Add(relativeFilePath);
                    directories.Add(GetRelativeDirectoryPath(rootPath, filePath));
                    break;
            }
        }
    }

    private static bool ShouldIgnoreDirectory(string directoryName)
    {
        if (ScannerDefaults.IgnoredDirectoryNames.Contains(directoryName))
        {
            return true;
        }

        return directoryName.StartsWith(ScannerDefaults.CMakeBuildPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private string GetRelativeDirectoryPath(string rootPath, string filePath)
    {
        var fullDirectoryPath = Path.GetDirectoryName(filePath) ?? rootPath;
        return _pathNormalizer.ToRelativeForwardSlashPath(rootPath, fullDirectoryPath);
    }
}
