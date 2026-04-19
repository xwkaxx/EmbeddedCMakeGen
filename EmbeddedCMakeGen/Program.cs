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

        var defaultProjectRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
        Console.WriteLine("Project root path:");
        Console.WriteLine("  - This is the folder of your embedded source project.");
        Console.WriteLine("  - Absolute paths are recommended.");
        Console.WriteLine($"  - Press Enter to use current working directory: {defaultProjectRoot}");
        Console.Write("> ");

        var rootInput = (Console.ReadLine() ?? string.Empty).Trim();
        var rootPath = NormalizeAbsolutePath(string.IsNullOrWhiteSpace(rootInput) ? defaultProjectRoot : rootInput);

        Console.WriteLine();
        Console.WriteLine("Output path:");
        Console.WriteLine("  - This is where generated CMake/environment files will be written.");
        Console.WriteLine("  - Absolute paths are recommended.");
        Console.WriteLine("  - Press Enter to use the project root path.");
        Console.Write("> ");

        var outputInput = (Console.ReadLine() ?? string.Empty).Trim();
        var outputPath = NormalizeAbsolutePath(string.IsNullOrWhiteSpace(outputInput) ? rootPath : outputInput);

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

        Console.WriteLine();
        Console.WriteLine("Select target platform:");
        Console.WriteLine("  1. STM32");
        Console.WriteLine("  2. Generic embedded C (reserved / future)");
        Console.Write("> ");

        var platformSelection = (Console.ReadLine() ?? string.Empty).Trim();
        var platformValue = platformSelection switch
        {
            "1" => "stm32",
            "2" => "generic",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(platformValue))
        {
            PrintError("Invalid platform selection. Please choose 1 or 2.");
            Pause();
            continue;
        }

        if (platformValue == "generic")
        {
            Console.WriteLine("Generic embedded C support is reserved for future expansion and is not yet fully implemented.");
            Console.WriteLine("Proceeding with best-effort generation behavior.");
        }

        string[] commandArgs;
        switch (selection)
        {
            case "1":
                commandArgs = ["scan", "--root", rootPath, "--platform", platformValue];
                break;
            case "2":
                commandArgs = ["preview", "--root", rootPath, "--out", outputPath, "--platform", platformValue];
                break;
            case "3":
                commandArgs = ["generate", "--root", rootPath, "--out", outputPath, "--platform", platformValue];
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
    Console.WriteLine("Embedded C/CMake Environment Generator");
    Console.WriteLine("Primary supported platform: STM32");
    Console.WriteLine("Long-term goal: multi-platform embedded projects");
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

static string NormalizeAbsolutePath(string path)
{
    return Path.GetFullPath(path);
}
