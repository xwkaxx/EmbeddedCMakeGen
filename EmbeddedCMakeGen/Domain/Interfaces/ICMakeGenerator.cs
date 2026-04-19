using EmbeddedCMakeGen.Domain.Models;

namespace EmbeddedCMakeGen.Domain.Interfaces;

public interface ICMakeGenerator
{
    void Generate(ProjectModel projectModel, string outputRootPath);
}
