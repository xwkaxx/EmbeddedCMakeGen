using EmbeddedCMakeGen.Commands;

if (args.Length == 0)
{
    return RunInteractiveMode();
}

var dispatcher = new CommandDispatcher();
var exitCode = dispatcher.Dispatch(args);

return exitCode;

static int RunInteractiveMode()
{
    var dispatcher = new CommandDispatcher();

    while (true)
    {
        PrintBanner();

        Console.Write("Enter project root path: ");
        var rootPath = (Console.ReadLine() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            PrintError("Project root path cannot be empty.");
            Pause();
            continue;
        }

        Console.Write("Enter output path (press Enter to use root): ");
        var outputPathInput = (Console.ReadLine() ?? string.Empty).Trim();
        var outputPath = string.IsNullOrWhiteSpace(outputPathInput) ? rootPath : outputPathInput;

        Console.WriteLine();
        Console.WriteLine("Select an action:");
        Console.WriteLine("  1. Scan project");
        Console.WriteLine("  2. Preview CMake files");
        Console.WriteLine("  3. Generate CMake files");
        Console.WriteLine("  q. Quit");
        Console.Write("> ");

        var selection = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
        if (selection == "q")
        {
            Console.WriteLine("Goodbye.");
            return 0;
        }

        string[] commandArgs;
        switch (selection)
        {
            case "1":
                commandArgs = ["scan", "--root", rootPath];
                break;
            case "2":
                commandArgs = ["preview", "--root", rootPath, "--out", outputPath];
                break;
            case "3":
                commandArgs = ["generate", "--root", rootPath, "--out", outputPath];
                break;
            default:
                PrintError("Invalid selection. Please choose 1, 2, 3, or q.");
                Pause();
                continue;
        }

        Console.WriteLine();
        Console.WriteLine("----------------------------------------");
        var exitCode = dispatcher.Dispatch(commandArgs);
        Console.WriteLine("----------------------------------------");
        Console.WriteLine(exitCode == 0 ? "Action completed successfully." : "Action failed. See messages above.");
        Pause();
    }
}

static void PrintBanner()
{
    Console.Clear();
    Console.WriteLine("========================================");
    Console.WriteLine("EmbeddedCMakeGen V1");
    Console.WriteLine("STM32 CMake Environment Generator");
    Console.WriteLine("========================================");
    Console.WriteLine();
    Console.WriteLine("Usage examples:");
    Console.WriteLine("  scan");
    Console.WriteLine("  preview");
    Console.WriteLine("  generate");
    Console.WriteLine();
}

static void PrintError(string message)
{
    Console.WriteLine($"[Error] {message}");
}

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press Enter to continue...");
    Console.ReadLine();
}
