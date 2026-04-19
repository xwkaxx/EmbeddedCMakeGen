namespace EmbeddedCMakeGen.Infrastructure.Logging;

public sealed class ConsoleLogger
{
    public void Log(string message)
    {
        System.Console.WriteLine(message);
    }
}
