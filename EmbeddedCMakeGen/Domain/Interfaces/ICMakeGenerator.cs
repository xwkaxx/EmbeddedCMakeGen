using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Domain.Interfaces;

public interface ICMakeGenerator
{
    GeneratedProjectFiles Generate(ProjectModel projectModel);
}
