using EmbeddedCMakeGen.Domain.Interfaces;

namespace EmbeddedCMakeGen.Infrastructure.Logging;

public sealed class ConsoleLogger : ILogger
{
    public void Info(string message)
    {
        Console.WriteLine(message);
    }

    public void Warn(string message)
    {
        Console.WriteLine($"WARN: {message}");
    }

    public void Error(string message)
    {
        Console.Error.WriteLine($"ERROR: {message}");
    }
}
