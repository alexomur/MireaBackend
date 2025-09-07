using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AdminPanel.Pages;
using Utils;

namespace AdminPanel.Features;

public class WebServer
{
    public async Task StartAsync(string url)
    {
        int port = 8080;
        try
        {
            Uri u = new Uri(url);
            if (u.Port > 0) port = u.Port;
        }
        catch { }
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Log.Info("Server started at " + url);
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            try { listener.Stop(); } catch { }
            Environment.Exit(0);
        };
        try
        {
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClient(client));
            }
        }
        catch (ObjectDisposedException)
        {
            Log.Info("Listener stopped");
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        using (client)
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream, Encoding.ASCII, false, 8192, true);
            StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 8192, true);
            writer.NewLine = "\r\n";
            try
            {
                string requestLine = await reader.ReadLineAsync() ?? "";
                if (requestLine.Length == 0) return;
                string method;
                string rawTarget;
                string httpVersion;
                string[] parts = requestLine.Split(' ');
                if (parts.Length < 3) { await WriteError(writer, 400, "Bad Request"); return; }
                method = parts[0];
                rawTarget = parts[1];
                httpVersion = parts[2];
                Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                while (true)
                {
                    string line = await reader.ReadLineAsync() ?? "";
                    if (line.Length == 0) break;
                    int idx = line.IndexOf(':');
                    if (idx > 0)
                    {
                        string name = line.Substring(0, idx).Trim();
                        string value = line.Substring(idx + 1).Trim();
                        headers[name] = value;
                    }
                }
                string host = headers.ContainsKey("Host") ? headers["Host"] : "localhost:" + "8080";
                string baseUrl = "http://" + host + "/";
                string path;
                string query;
                int qpos = rawTarget.IndexOf('?');
                if (qpos >= 0)
                {
                    path = rawTarget.Substring(0, qpos);
                    query = rawTarget.Substring(qpos + 1);
                }
                else
                {
                    path = rawTarget;
                    query = "";
                }
                if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteError(writer, 405, "Method Not Allowed");
                    return;
                }
                if (path == "/")
                {
                    string html = HtmlPage.BuildHome(baseUrl, Commands.AllowedKeys);
                    await WriteHtml(writer, 200, html);
                    Log.Info("Served admin home");
                    return;
                }
                if (string.Equals(path, "/exec", StringComparison.OrdinalIgnoreCase))
                {
                    Dictionary<string, string> qs = ParseQuery(query);
                    string key = qs.ContainsKey("key") ? qs["key"] : "";
                    string pathArg = qs.ContainsKey("path") ? qs["path"] : "";
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        await WriteError(writer, 400, "Query parameter 'key' is required");
                        return;
                    }
                    if (!Commands.IsAllowed(key))
                    {
                        await WriteError(writer, 400, "Unknown or disallowed command key");
                        return;
                    }
                    CommandSpec spec = Commands.Build(key, pathArg);
                    CommandResult result = await CommandRunner.RunShell(spec.Command);
                    string html = HtmlPage.BuildResult(spec.Title, spec.Command, result.StdOut, result.StdErr, result.ExitCode, result.ElapsedMilliseconds);
                    await WriteHtml(writer, 200, html);
                    string msg = spec.Title + " exited " + result.ExitCode.ToString(CultureInfo.InvariantCulture) + " in " + result.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + "ms";
                    Log.Info(msg);
                    return;
                }
                if (string.Equals(path, "/shell", StringComparison.OrdinalIgnoreCase))
                {
                    Dictionary<string, string> qs = ParseQuery(query);
                    string q = qs.ContainsKey("q") ? qs["q"] : "";
                    string token = qs.ContainsKey("token") ? qs["token"] : "";
                    if (string.IsNullOrWhiteSpace(q))
                    {
                        string page0 = HtmlPage.BuildShell(baseUrl, "", "", "", "", 0, 0, "");
                        await WriteHtml(writer, 200, page0);
                        return;
                    }
                    string envToken = Environment.GetEnvironmentVariable("ADMINPANEL_TOKEN") ?? "";
                    IPEndPoint? rep = client.Client.RemoteEndPoint as IPEndPoint;
                    bool isLoopback = rep != null && IPAddress.IsLoopback(rep.Address);
                    if (!string.IsNullOrEmpty(envToken))
                    {
                        if (string.IsNullOrEmpty(token) || !string.Equals(token, envToken, StringComparison.Ordinal))
                        {
                            await WriteError(writer, 403, "Forbidden");
                            return;
                        }
                    }
                    else
                    {
                        if (!isLoopback)
                        {
                            await WriteError(writer, 403, "Forbidden");
                            return;
                        }
                    }
                    ShellBuildResult build = ShellSafeBuilder.TryBuild(q);
                    if (!build.Ok)
                    {
                        string bad = HtmlPage.BuildShell(baseUrl, q, "", "", "Rejected: " + build.Error, -1, 0, build.Hint);
                        await WriteHtml(writer, 400, bad);
                        return;
                    }
                    CommandResult result = await CommandRunner.RunShell(build.Command);
                    string page = HtmlPage.BuildShell(baseUrl, q, build.Title, build.Command, result.StdErr, result.ExitCode, result.ElapsedMilliseconds, result.StdOut);
                    await WriteHtml(writer, 200, page);
                    Log.Info("Shell: \"" + q + "\" -> " + build.Command);
                    return;
                }
                await WriteError(writer, 404, "Not Found");
            }
            catch (Exception e)
            {
                try { await WriteError(writer, 500, e.Message); } catch { }
                Log.Error(e.Message);
            }
        }
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return dict;
        string[] pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < pairs.Length; i++)
        {
            string p = pairs[i];
            int eq = p.IndexOf('=');
            if (eq >= 0)
            {
                string k = WebUtility.UrlDecode(p.Substring(0, eq));
                string v = WebUtility.UrlDecode(p.Substring(eq + 1));
                if (!dict.ContainsKey(k)) dict[k] = v;
            }
            else
            {
                string k = WebUtility.UrlDecode(p);
                if (!dict.ContainsKey(k)) dict[k] = "";
            }
        }
        return dict;
    }

    private static async Task WriteHtml(StreamWriter writer, int status, string html)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(html);
        string statusText = GetStatusText(status);
        await writer.WriteLineAsync("HTTP/1.1 " + status.ToString(CultureInfo.InvariantCulture) + " " + statusText);
        await writer.WriteLineAsync("Content-Type: text/html; charset=utf-8");
        await writer.WriteLineAsync("Content-Length: " + bytes.Length.ToString(CultureInfo.InvariantCulture));
        await writer.WriteLineAsync("Connection: close");
        await writer.WriteLineAsync("Date: " + DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture));
        await writer.WriteLineAsync("Server: AdminPanel-Tcp");
        await writer.WriteLineAsync();
        await writer.FlushAsync();
        await writer.BaseStream.WriteAsync(bytes, 0, bytes.Length);
        await writer.BaseStream.FlushAsync();
    }

    private static async Task WriteError(StreamWriter writer, int status, string message)
    {
        string body = "<html><head><meta charset=\"utf-8\"><title>Error " + status.ToString(CultureInfo.InvariantCulture) + "</title></head><body style=\"font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif;\"><h1>Error " + status.ToString(CultureInfo.InvariantCulture) + "</h1><p>" + WebUtility.HtmlEncode(message) + "</p></body></html>";
        await WriteHtml(writer, status, body);
    }

    private static string GetStatusText(int status)
    {
        if (status == 200) return "OK";
        if (status == 400) return "Bad Request";
        if (status == 403) return "Forbidden";
        if (status == 404) return "Not Found";
        if (status == 405) return "Method Not Allowed";
        if (status == 500) return "Internal Server Error";
        return "Status";
    }
}
