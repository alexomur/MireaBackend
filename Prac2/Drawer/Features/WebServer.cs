using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Drawer.Enums;
using Utils;

namespace Drawer.Features;

public class WebServer(string? wwwRoot = null)
{
    private readonly string _wwwRoot = wwwRoot ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    private int _port;

    public async Task StartAsync(string url)
    {
        try
        {
            if (!Directory.Exists(_wwwRoot)) 
                Directory.CreateDirectory(_wwwRoot);
            string urlFixed = url.EndsWith('/') ? url : url + '/';
            int port = 8080;
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
            Log.Info("Drawer started at http://" + logHost + ":" + port.ToString(CultureInfo.InvariantCulture) + "/");
            Log.Info("Root dir: " + _wwwRoot);
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
                Log.Error("Server error: " + ex.Message);
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private static IPAddress ResolveBindAddress(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) 
            return IPAddress.Loopback;
        return host switch
        {
            "127.0.0.1" or "::1" => IPAddress.Loopback,
            "0.0.0.0" or "*" or "+" => IPAddress.Any,
            _ => IPAddress.TryParse(host, out IPAddress? ip) ? ip : IPAddress.Any
        };
    }

    private static string ResolveLogHost(string host, IPAddress bind)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) 
            return "localhost";
        switch (host)
        {
            case "127.0.0.1" or "::1":
                return "localhost";
            case "0.0.0.0" or "*" or "+":
                return "0.0.0.0";
        }

        if (IPAddress.TryParse(host, out IPAddress? ip)) 
            return ip.ToString();
        return Equals(bind, IPAddress.Any) ? "0.0.0.0" : bind.ToString();
    }

    private async Task HandleClient(TcpClient client)
    {
        using (client)
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new(stream, Encoding.UTF8, false, 8192, true);
            StreamWriter writer = new(stream, new UTF8Encoding(false), 8192, true);
            writer.NewLine = "\r\n";
            try
            {
                string requestLine = await reader.ReadLineAsync() ?? "";
                if (requestLine.Length == 0) 
                    return;
                string[] first = requestLine.Split(' ');
                if (first.Length < 3)
                {
                    await WriteError(writer, 400, "Bad Request");
                    return;
                }
                string method = first[0];
                string rawTarget = first[1];
                Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
                while (true)
                {
                    string line = await reader.ReadLineAsync() ?? "";
                    if (line.Length == 0) 
                        break;
                    
                    int idx = line.IndexOf(':');
                    if (idx <= 0) 
                        continue;
                    
                    string name = line[..idx].Trim();
                    string value = line[(idx + 1)..].Trim();
                    headers[name] = value;
                }
                string host = headers.TryGetValue("Host", out string? header) ? header : "localhost:" + _port.ToString(CultureInfo.InvariantCulture);
                string baseUrl = "http://" + host + "/";
                string path;
                string query;
                int qpos = rawTarget.IndexOf('?');
                if (qpos >= 0)
                {
                    path = rawTarget[..qpos];
                    query = rawTarget[(qpos + 1)..];
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
                    Log.Info("Served homepage");
                    return;
                }
                if (string.Equals(path, "/drawer", StringComparison.OrdinalIgnoreCase) || string.Equals(path, "/drawer.svg", StringComparison.OrdinalIgnoreCase))
                {
                    Dictionary<string, string> qs = ParseQuery(query);
                    int shapeRaw;
                    bool okShape = TryGetInt(qs, "shape", 0, out shapeRaw);
                    if (!okShape)
                    {
                        await WriteError(writer, 400, "Parameter 'shape' must be an integer");
                        return;
                    }
                    ShapeType shape = shapeRaw == 1 ? ShapeType.Circle :
                                      shapeRaw == 2 ? ShapeType.Rectangle :
                                      shapeRaw == 3 ? ShapeType.Triangle :
                                      shapeRaw == 4 ? ShapeType.Star : ShapeType.None;
                    if (shape == ShapeType.None)
                    {
                        await WriteError(writer, 400, "Unknown shape");
                        return;
                    }
                    int colorIndex;
                    bool okColor = TryGetInt(qs, "color", 0, out colorIndex);
                    if (!okColor)
                    {
                        await WriteError(writer, 400, "Parameter 'color' must be an integer");
                        return;
                    }
                    int width;
                    bool okW = TryGetInt(qs, "width", 200, out width);
                    if (!okW || width <= 0)
                    {
                        await WriteError(writer, 400, "Parameter 'width' must be a positive integer");
                        return;
                    }
                    int height;
                    bool okH = TryGetInt(qs, "height", 200, out height);
                    if (!okH || height <= 0)
                    {
                        await WriteError(writer, 400, "Parameter 'height' must be a positive integer");
                        return;
                    }
                    int stroke;
                    bool okS = TryGetInt(qs, "stroke", 2, out stroke);
                    if (!okS || stroke < 0)
                    {
                        await WriteError(writer, 400, "Parameter 'stroke' must be a non-negative integer");
                        return;
                    }
                    int padding;
                    bool okP = TryGetInt(qs, "padding", 0, out padding);
                    if (!okP || padding < 0)
                    {
                        await WriteError(writer, 400, "Parameter 'padding' must be a non-negative integer");
                        return;
                    }
                    string svg = SvgRenderer.Render(shape, colorIndex, width, height, stroke, padding);
                    await WriteResponse(writer, 200, "image/svg+xml; charset=utf-8", Encoding.UTF8.GetBytes(svg));
                    Log.Info("Served SVG " + shape.ToString() + " " + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture));
                    return;
                }
                string filePath = Path.Combine(_wwwRoot, path.TrimStart('/'));
                filePath = Path.GetFullPath(filePath);
                if (!filePath.StartsWith(_wwwRoot, StringComparison.OrdinalIgnoreCase))
                {
                    await WriteError(writer, 403, "Access denied");
                    return;
                }
                if (File.Exists(filePath))
                {
                    byte[] content = await File.ReadAllBytesAsync(filePath);
                    string mime = ContentTypes.GetMimeType(filePath);
                    await WriteResponse(writer, 200, mime, content);
                    Log.Info("Served file: " + filePath);
                    return;
                }
                await WriteError(writer, 404, "File not found");
            }
            catch (Exception e)
            {
                try { await WriteError(writer, 500, "Internal server error"); } catch { }
                Log.Error(e.Message);
            }
        }
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return dict;
        string[] pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (string p in pairs)
        {
            int eq = p.IndexOf('=');
            if (eq >= 0)
            {
                string k = WebUtility.UrlDecode(p.Substring(0, eq));
                string v = WebUtility.UrlDecode(p.Substring(eq + 1));
                dict.TryAdd(k, v);
            }
            else
            {
                string k = WebUtility.UrlDecode(p);
                dict.TryAdd(k, "");
            }
        }
        return dict;
    }

    private static bool TryGetInt(Dictionary<string, string> qs, string name, int defaultValue, out int value)
    {
        if (!qs.TryGetValue(name, out string? raw) || string.IsNullOrEmpty(raw))
        {
            value = defaultValue;
            return true;
        }
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static async Task WriteResponse(StreamWriter writer, int status, string contentType, byte[] body)
    {
        string statusText = GetStatusText(status);
        await writer.WriteLineAsync("HTTP/1.1 " + status.ToString(CultureInfo.InvariantCulture) + " " + statusText);
        await writer.WriteLineAsync("Content-Type: " + contentType);
        await writer.WriteLineAsync("Content-Length: " + body.Length.ToString(CultureInfo.InvariantCulture));
        await writer.WriteLineAsync("Connection: close");
        await writer.WriteLineAsync("Date: " + DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture));
        await writer.WriteLineAsync("Server: Drawer-Tcp");
        await writer.WriteLineAsync();
        await writer.FlushAsync();
        await writer.BaseStream.WriteAsync(body);
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
