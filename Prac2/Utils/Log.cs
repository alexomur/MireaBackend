using System.Reflection;

namespace Utils;

public static class Log
{
    private static string LogDir => Path.Combine(Directory.GetCurrentDirectory(), "logs");
    
    private static string CurrentLogFile => Path.Combine(LogDir, $"server_{DateTime.Now:yyyy-MM-dd-hh}.log");
    
    private static ConsoleColor DefaultColor => ConsoleColor.Gray;
    
    private static object LockObject = new();

    static Log()
    {
        if (!Directory.Exists(LogDir))
        {
            Directory.CreateDirectory(LogDir);
        }
    }

    private static void WriteToFile(string message)
    {
        lock (LockObject)
        {
            try
            {
                File.AppendAllText(CurrentLogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log: {ex.Message}");
            }
        }
    }

    private static void WriteToConsole(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        Console.ForegroundColor = DefaultColor;
    }

    public static void Raw(string message, ConsoleColor color = ConsoleColor.Gray)
    {
        WriteToConsole(message, color);
        WriteToFile(message);
    }

    public static void Info(string message)
    {
        Raw($"[{Assembly.GetCallingAssembly().GetName().Name}] [INFO] {message}", ConsoleColor.Cyan);
    }
    
    public static void Warn(string message)
    {
        Raw($"[{Assembly.GetCallingAssembly().GetName().Name}] [WARN] {message}", ConsoleColor.Yellow);
    }
    
    public static void Error(string message)
    {
        Raw($"[{Assembly.GetCallingAssembly().GetName().Name}] [ERROR] {message}", ConsoleColor.Red);
    }
}