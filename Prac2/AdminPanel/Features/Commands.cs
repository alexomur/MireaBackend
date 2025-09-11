using System;
using System.Text.RegularExpressions;

namespace AdminPanel.Features;

public static class Commands
{
    private static readonly System.Collections.Generic.HashSet<string> Keys = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "whoami","id","uname","uptime","df","free","ps","ls","env","pwd","hostname"
    };

    public static System.Collections.Generic.IEnumerable<string> AllowedKeys => Keys;

    public static bool IsAllowed(string key)
    {
        return Keys.Contains(key);
    }

    public static CommandSpec Build(string key, string path)
    {
        bool isWindows = OperatingSystem.IsWindows();
        if (string.Equals(key, "whoami", StringComparison.OrdinalIgnoreCase)) return new CommandSpec("whoami", "whoami");
        if (string.Equals(key, "id", StringComparison.OrdinalIgnoreCase)) return isWindows ? new CommandSpec("whoami /all", "whoami /all") : new CommandSpec("id", "id");
        if (string.Equals(key, "uname", StringComparison.OrdinalIgnoreCase)) return isWindows ? new CommandSpec("system info", "[System.Environment]::OSVersion | Format-List *") : new CommandSpec("uname -a", "uname -a");
        if (string.Equals(key, "uptime", StringComparison.OrdinalIgnoreCase)) return isWindows ? new CommandSpec("uptime", "(Get-Date) - (Get-CimInstance Win32_OperatingSystem).LastBootUpTime | Out-String") : new CommandSpec("uptime", "uptime");
        if (string.Equals(key, "df", StringComparison.OrdinalIgnoreCase)) return isWindows ? new CommandSpec("drives", "Get-PSDrive -PSProvider FileSystem | Format-Table -AutoSize") : new CommandSpec("df -h", "df -h");
        if (string.Equals(key, "free", StringComparison.OrdinalIgnoreCase)) return isWindows ? new CommandSpec("memory", "Get-CimInstance Win32_OperatingSystem | Select-Object TotalVisibleMemorySize, FreePhysicalMemory | Format-List") : new CommandSpec("free -m", "free -m");
        if (string.Equals(key, "ps", StringComparison.OrdinalIgnoreCase)) return isWindows ? new CommandSpec("processes", "Get-Process | Sort-Object CPU -Descending | Select-Object -First 200 | Format-Table -AutoSize") : new CommandSpec("ps aux", "ps aux");
        if (string.Equals(key, "env", StringComparison.OrdinalIgnoreCase)) return isWindows ? new CommandSpec("env", "Get-ChildItem env:* | Sort-Object Name | Format-Table -AutoSize") : new CommandSpec("env", "env");
        if (string.Equals(key, "pwd", StringComparison.OrdinalIgnoreCase)) return isWindows ? new CommandSpec("pwd", "Get-Location") : new CommandSpec("pwd", "pwd");
        if (string.Equals(key, "hostname", StringComparison.OrdinalIgnoreCase)) return new CommandSpec("hostname", "hostname");
        if (string.Equals(key, "ls", StringComparison.OrdinalIgnoreCase))
        {
            string safe = SanitizePath(path, isWindows);
            if (string.IsNullOrWhiteSpace(safe)) safe = ".";
            if (isWindows)
            {
                string cmdW = "Get-ChildItem -Force -LiteralPath " + ShellQuotes.PwshQuote(safe) + " | Format-Table -AutoSize";
                string titleW = "ls -la " + safe;
                return new CommandSpec(titleW, cmdW);
            }
            else
            {
                string cmd = "ls -la -- " + ShellQuotes.BashQuote(safe);
                string title = "ls -la " + safe;
                return new CommandSpec(title, cmd);
            }
        }
        return new CommandSpec(key, key);
    }

    private static string SanitizePath(string input, bool isWindows)
    {
        if (input == null) return "";
        string s = input.Trim();
        Regex r = isWindows ? new Regex("^[A-Za-z0-9_\\-./~ \\\\:]{1,4096}$") : new Regex("^[A-Za-z0-9_\\-./~ ]{1,4096}$");
        if (!r.IsMatch(s)) return "";
        return s;
    }
}

public readonly struct CommandSpec
{
    public string Title { get; }
    public string Command { get; }

    public CommandSpec(string title, string command)
    {
        Title = title;
        Command = command;
    }
}
