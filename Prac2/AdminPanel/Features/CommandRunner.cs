using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AdminPanel.Features;

public static class CommandRunner
{
    public static async Task<CommandResult> RunShell(string command)
    {
        bool isWindows = OperatingSystem.IsWindows();
        ProcessStartInfo psi = new ProcessStartInfo();
        if (isWindows)
        {
            string escaped = command.Replace("`", "``").Replace("\"", "`\"");
            psi.FileName = "powershell";
            psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + escaped + "\"";
        }
        else
        {
            string shell = File.Exists("/bin/sh") ? "/bin/sh" : "sh";
            psi.FileName = shell;
            psi.Arguments = "-lc \"" + command.Replace("\"", "\\\"") + "\"";
        }
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.StandardOutputEncoding = Encoding.UTF8;
        psi.StandardErrorEncoding = Encoding.UTF8;
        Process p = new Process();
        p.StartInfo = psi;
        DateTime start = DateTime.UtcNow;
        p.Start();
        string stdout = await p.StandardOutput.ReadToEndAsync();
        string stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        int code = p.ExitCode;
        long ms = (long)(DateTime.UtcNow - start).TotalMilliseconds;
        CommandResult result = new CommandResult(code, stdout, stderr, ms);
        return result;
    }
}

public readonly struct CommandResult
{
    public int ExitCode { get; }
    public string StdOut { get; }
    public string StdErr { get; }
    public long ElapsedMilliseconds { get; }

    public CommandResult(int exitCode, string stdOut, string stdErr, long elapsedMilliseconds)
    {
        ExitCode = exitCode;
        StdOut = stdOut;
        StdErr = stdErr;
        ElapsedMilliseconds = elapsedMilliseconds;
    }
}
