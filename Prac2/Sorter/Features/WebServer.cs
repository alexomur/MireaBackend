using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Utils;

namespace Sorter.Features;

public class WebServer(string? wwwRoot = null)
{
    private readonly string _wwwRoot = wwwRoot ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    private int _port;

    public async Task StartAsync(string url)
    {
        try
        {
            if (!Directory.Exists(_wwwRoot)) Directory.CreateDirectory(_wwwRoot);
            string urlFixed = url.EndsWith("/") ? url : url + "/";
            int port = 8081;
            string host = "0.0.0.0";
            try
            {
                Uri u = new(urlFixed);
                if (u.Port > 0) port = u.Port;
                host = u.Host;
            }
            catch
            {
            }
            _port = port;
            IPAddress bindAddress = ResolveBindAddress(host);
            TcpListener listener = new(bindAddress, port);
            listener.Start();
            string logHost = ResolveLogHost(host, bindAddress);
            Log.Info("Server started at http://" + logHost + ":" + port.ToString(CultureInfo.InvariantCulture) + "/");
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
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private static IPAddress ResolveBindAddress(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return IPAddress.Loopback;
        if (host == "127.0.0.1" || host == "::1") return IPAddress.Loopback;
        if (host == "0.0.0.0" || host == "*" || host == "+") return IPAddress.Any;
        if (IPAddress.TryParse(host, out IPAddress? ip)) return ip;
        return IPAddress.Any;
    }

    private static string ResolveLogHost(string host, IPAddress bind)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return "localhost";
        if (host == "127.0.0.1" || host == "::1") return "localhost";
        if (host == "0.0.0.0" || host == "*" || host == "+") return "0.0.0.0";
        if (IPAddress.TryParse(host, out IPAddress? ip)) return ip.ToString();
        if (Equals(bind, IPAddress.Any)) return "0.0.0.0";
        return bind.ToString();
    }

    private async Task HandleClient(TcpClient client)
    {
        using (client)
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 8192, true);
            StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 8192, true);
            writer.NewLine = "\r\n";
            try
            {
                string requestLine = await reader.ReadLineAsync() ?? "";
                if (requestLine.Length == 0) return;
                string[] first = requestLine.Split(' ');
                if (first.Length < 3)
                {
                    await WriteError(writer, 400, "Bad Request");
                    return;
                }
                string method = first[0];
                string rawTarget = first[1];
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
                string host = headers.ContainsKey("Host") ? headers["Host"] : "localhost:" + _port.ToString(CultureInfo.InvariantCulture);
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
                string remote = client.Client.RemoteEndPoint != null ? client.Client.RemoteEndPoint.ToString() ?? "" : "";
                Log.Info(method + " " + rawTarget + " from " + remote);
                if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteError(writer, 405, "Method Not Allowed");
                    return;
                }
                if (path == "/")
                {
                    string html = HtmlPage.BuildHome(baseUrl);
                    await WriteResponse(writer, 200, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html));
                    return;
                }
                if (string.Equals(path, "/sort", StringComparison.OrdinalIgnoreCase))
                {
                    Dictionary<string, string> qs = ParseQuery(query);
                    if (!qs.TryGetValue("array", out string? src) || string.IsNullOrWhiteSpace(src))
                    {
                        await WriteError(writer, 400, "Query parameter 'array' is required");
                        return;
                    }
                    string[] rawParts = src.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    int[] numbers = new int[rawParts.Length];
                    for (int i = 0; i < rawParts.Length; i++)
                    {
                        string piece = rawParts[i].Trim();
                        int val;
                        bool ok = int.TryParse(piece, NumberStyles.Integer, CultureInfo.InvariantCulture, out val);
                        if (!ok)
                        {
                            await WriteError(writer, 400, "All elements must be integers");
                            return;
                        }
                        numbers[i] = val;
                    }
                    int[] sorted = MergeSorter.Sort(numbers);
                    string[] texts = new string[sorted.Length];
                    for (int i = 0; i < sorted.Length; i++) texts[i] = sorted[i].ToString(CultureInfo.InvariantCulture);
                    string result = string.Join(", ", texts);
                    string html = HtmlPage.BuildResult(src, result);
                    await WriteResponse(writer, 200, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html));
                    Log.Info("Sorted " + numbers.Length.ToString(CultureInfo.InvariantCulture) + " numbers");
                    return;
                }
                string filePath = Path.Combine(_wwwRoot, path.TrimStart('/'));
                string full = Path.GetFullPath(filePath);
                if (!full.StartsWith(_wwwRoot, StringComparison.OrdinalIgnoreCase))
                {
                    await WriteError(writer, 403, "Access denied");
                    return;
                }
                if (File.Exists(full))
                {
                    byte[] content = await File.ReadAllBytesAsync(full);
                    string mime = Utils.ContentTypes.GetMimeType(full);
                    string ct = NeedsUtf8(mime) ? mime + "; charset=utf-8" : mime;
                    await WriteResponse(writer, 200, ct, content);
                    Log.Info("Served file: " + full);
                    return;
                }
                await WriteError(writer, 404, "Not Found");
            }
            catch (Exception e)
            {
                try { await WriteError(writer, 500, "Internal Server Error"); } catch { }
                Log.Error(e.Message);
            }
        }
    }

    private static bool NeedsUtf8(string mime)
    {
        if (mime.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(mime, "application/javascript", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(mime, "application/json", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(mime, "image/svg+xml", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
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

    private static async Task WriteResponse(StreamWriter writer, int status, string contentType, byte[] body)
    {
        string statusText = GetStatusText(status);
        await writer.WriteLineAsync("HTTP/1.1 " + status.ToString(CultureInfo.InvariantCulture) + " " + statusText);
        await writer.WriteLineAsync("Content-Type: " + contentType);
        await writer.WriteLineAsync("Content-Length: " + body.Length.ToString(CultureInfo.InvariantCulture));
        await writer.WriteLineAsync("Connection: close");
        await writer.WriteLineAsync("Date: " + DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture));
        await writer.WriteLineAsync("Server: Sorter-Tcp");
        await writer.WriteLineAsync();
        await writer.FlushAsync();
        await writer.BaseStream.WriteAsync(body, 0, body.Length);
        await writer.BaseStream.FlushAsync();
    }

    private static async Task WriteError(StreamWriter writer, int status, string message)
    {
        string html = "<html><head><meta charset=\"utf-8\"><title>Error " + status.ToString(CultureInfo.InvariantCulture) + "</title></head><body style=\"font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif;\"><h1>Error " + status.ToString(CultureInfo.InvariantCulture) + "</h1><p>" + WebUtility.HtmlEncode(message) + "</p></body></html>";
        await WriteResponse(writer, status, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html));
    }

    private static string GetStatusText(int status)
    {
        return status switch
        {
            200 => "OK",
            400 => "Bad Request",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            500 => "Internal Server Error",
            _ => "Status"
        };
    }
}
