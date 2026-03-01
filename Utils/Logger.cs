using PropsRemover.Configs;

namespace PropsRemover.Utils;

public static class Logger
{
    private const string PluginName = "PropsRemover";
    public static BaseConfigs? Config { get; set; }

    public static void LogInfo(string category, string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{PluginName} - {category}] {message}");
        Console.ResetColor();
    }

    public static void LogWarning(string category, string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{PluginName} - {category}] {message}");
        Console.ResetColor();
    }

    public static void LogError(string category, string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{PluginName} - {category}] {message}");
        Console.ResetColor();
    }

    public static void LogDebug(string category, string message)
    {
        if (Config?.EnableDebug != true) return;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{PluginName} - {category}] {message}");
        Console.ResetColor();
    }
}