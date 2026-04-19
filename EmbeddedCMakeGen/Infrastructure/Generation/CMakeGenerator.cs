using EmbeddedCMakeGen.Domain.Interfaces;
using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Infrastructure.Generation;

public sealed class CMakeGenerator : ICMakeGenerator
{
    public GeneratedProjectFiles Generate(ProjectModel projectModel)
    {
        return GeneratedProjectFiles.Empty;
    }
}
