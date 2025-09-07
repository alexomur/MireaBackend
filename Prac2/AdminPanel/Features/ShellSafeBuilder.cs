using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AdminPanel.Features;

public static class ShellSafeBuilder
{
    private static readonly HashSet<string> UnixAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "whoami","id","uname","uptime","df","free","ps","env","pwd","hostname","ls","cat","head","tail"
    };

    private static readonly HashSet<string> WinAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "whoami","hostname","dir","type","Get-Process","Get-ChildItem","Get-Location","Get-ChildItemEnv","systeminfo","uptime","drives","memory"
    };

    private static readonly Regex SafeArgUnix = new Regex(@"^[A-Za-z0-9_\-./~@:+=,\s]{1,2048}$");
    private static readonly Regex SafeArgWin = new Regex(@"^[A-Za-z0-9_\-./~\\:@+=,\s]{1,2048}$");
    private static readonly char[] Ws = new[] { ' ', '\t', '\r', '\n' };

    public static ShellBuildResult TryBuild(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return ShellBuildResult.Fail("Empty", "Введите команду");
        string trimmed = input.Trim();
        if (trimmed.Length > 2048) return ShellBuildResult.Fail("Too long", "Слишком длинная команда");
        string[] parts = SplitOnce(trimmed);
        string cmd = parts[0];
        string rest = parts[1];
        bool isWindows = OperatingSystem.IsWindows();
        if (isWindows)
        {
            if (!SafeArgWin.IsMatch(trimmed)) return ShellBuildResult.Fail("Bad chars", "Недопустимые символы");
            if (string.Equals(cmd, "whoami", StringComparison.OrdinalIgnoreCase)) return Ok("whoami", "whoami", "whoami");
            if (string.Equals(cmd, "hostname", StringComparison.OrdinalIgnoreCase)) return Ok("hostname", "hostname", "hostname");
            if (string.Equals(cmd, "dir", StringComparison.OrdinalIgnoreCase) || string.Equals(cmd, "ls", StringComparison.OrdinalIgnoreCase))
            {
                string path = string.IsNullOrWhiteSpace(rest) ? "." : rest.Trim();
                string ps = "Get-ChildItem -Force -LiteralPath " + PwshQuote(path) + " | Format-Table -AutoSize";
                return Ok("ls -la " + path, ps, "ls");
            }
            if (string.Equals(cmd, "type", StringComparison.OrdinalIgnoreCase) || string.Equals(cmd, "cat", StringComparison.OrdinalIgnoreCase))
            {
                string path = rest.Trim();
                if (string.IsNullOrWhiteSpace(path)) return ShellBuildResult.Fail("Missing arg", "Нужен путь к файлу");
                string ps = "Get-Content -LiteralPath " + PwshQuote(path) + " -TotalCount 1000";
                return Ok("cat " + path, ps, "cat");
            }
            if (string.Equals(cmd, "ps", StringComparison.OrdinalIgnoreCase)) return Ok("processes", "Get-Process | Sort-Object CPU -Descending | Select-Object -First 200 | Format-Table -AutoSize", "ps");
            if (string.Equals(cmd, "pwd", StringComparison.OrdinalIgnoreCase)) return Ok("pwd", "Get-Location", "pwd");
            if (string.Equals(cmd, "env", StringComparison.OrdinalIgnoreCase)) return Ok("env", "Get-ChildItem env:* | Sort-Object Name | Format-Table -AutoSize", "env");
            if (string.Equals(cmd, "uptime", StringComparison.OrdinalIgnoreCase)) return Ok("uptime", "(Get-Date) - (Get-CimInstance Win32_OperatingSystem).LastBootUpTime | Out-String", "uptime");
            if (string.Equals(cmd, "df", StringComparison.OrdinalIgnoreCase)) return Ok("drives", "Get-PSDrive -PSProvider FileSystem | Format-Table -AutoSize", "df");
            if (string.Equals(cmd, "free", StringComparison.OrdinalIgnoreCase)) return Ok("memory", "Get-CimInstance Win32_OperatingSystem | Select-Object TotalVisibleMemorySize, FreePhysicalMemory | Format-List", "free");
            return ShellBuildResult.Fail("Not allowed", "Команда не разрешена");
        }
        else
        {
            if (!SafeArgUnix.IsMatch(trimmed)) return ShellBuildResult.Fail("Bad chars", "Недопустимые символы");
            if (!UnixAllowed.Contains(cmd)) return ShellBuildResult.Fail("Not allowed", "Команда не разрешена");
            if (string.Equals(cmd, "ls", StringComparison.OrdinalIgnoreCase))
            {
                string path = string.IsNullOrWhiteSpace(rest) ? "." : rest.Trim();
                string sh = "ls -la -- " + BashQuote(path);
                return Ok("ls -la " + path, sh, "ls");
            }
            if (string.Equals(cmd, "cat", StringComparison.OrdinalIgnoreCase))
            {
                string path = rest.Trim();
                if (string.IsNullOrWhiteSpace(path)) return ShellBuildResult.Fail("Missing arg", "Нужен путь к файлу");
                string sh = "head -c 200000 -- " + BashQuote(path);
                return Ok("cat " + path, sh, "cat");
            }
            if (string.Equals(cmd, "head", StringComparison.OrdinalIgnoreCase))
            {
                string path = rest.Trim();
                if (string.IsNullOrWhiteSpace(path)) return ShellBuildResult.Fail("Missing arg", "Нужен путь к файлу");
                string sh = "head -n 200 -- " + BashQuote(path);
                return Ok("head " + path, sh, "head");
            }
            if (string.Equals(cmd, "tail", StringComparison.OrdinalIgnoreCase))
            {
                string path = rest.Trim();
                if (string.IsNullOrWhiteSpace(path)) return ShellBuildResult.Fail("Missing arg", "Нужен путь к файлу");
                string sh = "tail -n 200 -- " + BashQuote(path);
                return Ok("tail " + path, sh, "tail");
            }
            if (string.Equals(cmd, "ps", StringComparison.OrdinalIgnoreCase)) return Ok("ps aux", "ps aux", "ps");
            if (string.Equals(cmd, "whoami", StringComparison.OrdinalIgnoreCase)) return Ok("whoami", "whoami", "whoami");
            if (string.Equals(cmd, "id", StringComparison.OrdinalIgnoreCase)) return Ok("id", "id", "id");
            if (string.Equals(cmd, "uname", StringComparison.OrdinalIgnoreCase)) return Ok("uname -a", "uname -a", "uname");
            if (string.Equals(cmd, "uptime", StringComparison.OrdinalIgnoreCase)) return Ok("uptime", "uptime", "uptime");
            if (string.Equals(cmd, "df", StringComparison.OrdinalIgnoreCase)) return Ok("df -h", "df -h", "df");
            if (string.Equals(cmd, "free", StringComparison.OrdinalIgnoreCase)) return Ok("free -m", "free -m", "free");
            if (string.Equals(cmd, "env", StringComparison.OrdinalIgnoreCase)) return Ok("env", "env", "env");
            if (string.Equals(cmd, "pwd", StringComparison.OrdinalIgnoreCase)) return Ok("pwd", "pwd", "pwd");
            if (string.Equals(cmd, "hostname", StringComparison.OrdinalIgnoreCase)) return Ok("hostname", "hostname", "hostname");
            return ShellBuildResult.Fail("Not allowed", "Команда не разрешена");
        }
    }

    private static ShellBuildResult Ok(string title, string command, string key)
    {
        return new ShellBuildResult(true, title, command, key, "", "");
    }

    private static string[] SplitOnce(string s)
    {
        int i = s.IndexOfAny(Ws);
        if (i < 0) return new[] { s, "" };
        string head = s.Substring(0, i);
        string tail = s.Substring(i).Trim();
        return new[] { head, tail };
    }

    private static string BashQuote(string s)
    {
        string t = s.Replace("'", "'\"'\"'");
        return "'" + t + "'";
    }

    private static string PwshQuote(string s)
    {
        string t = s.Replace("'", "''");
        return "'" + t + "'";
    }
}

public readonly struct ShellBuildResult
{
    public bool Ok { get; }
    public string Title { get; }
    public string Command { get; }
    public string Key { get; }
    public string Error { get; }
    public string Hint { get; }

    public ShellBuildResult(bool ok, string title, string command, string key, string error, string hint)
    {
        Ok = ok;
        Title = title;
        Command = command;
        Key = key;
        Error = error;
        Hint = hint;
    }

    public static ShellBuildResult Fail(string error, string hint)
    {
        return new ShellBuildResult(false, "", "", "", error, hint);
    }
}
