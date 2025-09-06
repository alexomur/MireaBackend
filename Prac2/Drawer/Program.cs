using System.Net;
using System.Text;
using Utils;

namespace Drawer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        WebServer server = new();
        await server.StartAsync("http://localhost:8080/");
    }
}

public class WebServer
{
    private readonly string _wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

    public async Task StartAsync(string url)
    {
        Log.Info("\n===\nSTARTING WEBSERVER\n===");
        if (!url.EndsWith('/'))
        {
            url += "/";
            Log.Warn($"URL changed to: {url}");
        }

        HttpListener listener = new();
        listener.Prefixes.Add(url);
        listener.Start();
        
        Log.Info($"Server started at {url}");
        Log.Info($"Root dir: {_wwwRoot}");

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Log.Info("Server shutting down");
            listener.Stop();
            Environment.Exit(0);
        };

        try
        {
            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                _ = Task.Run(() => ProcessRequest(context));
            }
        }
        catch (ObjectDisposedException)
        {
            Log.Info("Listener stopped");
        }
        catch (Exception ex)
        {
            Log.Error($"Server error: {ex.Message}");
        }
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        try
        {
            Log.Info($"{request.HttpMethod} {request.Url} from {request.RemoteEndPoint}");

            if (request.HttpMethod == "GET")
            {
                await GetRequest(request, response);
            }
            else if (request.HttpMethod == "POST")
            {
                Log.Warn("POST requests not supported");
                await Error(response, 501, "POST requests not supported");
            }
            else
            {
                Log.Warn($"Method {request.HttpMethod} not supported");
                await Error(response, 405, "Method not supported");
            }
        }
        catch (Exception e)
        {
            Log.Error($"{e.Message}\n{e.StackTrace}");
            await Error(response, 500, "Internal server error");
        }
        finally
        {
            response.Close();
        }
    }

    private async Task GetRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url == null ? "/index.html" : request.Url.AbsolutePath;

        if (path == "/")
        {
            path = "/index.html";
        }

        if (string.Equals(path, "/drawer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path, "/drawer.svg", StringComparison.OrdinalIgnoreCase))
        {
            await HandleDrawer(request, response);
            return;
        }
        
        string filePath = Path.Combine(_wwwRoot, path.TrimStart('/'));
        filePath = Path.GetFullPath(filePath);

        if (!filePath.StartsWith(_wwwRoot, StringComparison.OrdinalIgnoreCase))
        {
            await Error(response, 403, "Access denied");
            return;
        }

        if (File.Exists(filePath))
        {
            byte[] content = await File.ReadAllBytesAsync(filePath);
            response.ContentType = ContentTypes.GetMimeType(filePath);
            response.ContentLength64 = content.LongLength;
            await response.OutputStream.WriteAsync(content);
            Log.Info($"Served file: {filePath}");
        }
        else
        {
            await Error(response, 404, "File not found");
        }
    }

    private async Task HandleDrawer(HttpListenerRequest request, HttpListenerResponse response)
    {
        string numRaw = request.QueryString["num"];
        int num;
        bool ok = int.TryParse(numRaw, out num);
        if (!ok)
        {
            await Error(response, 400, "Query parameter 'num' must be an integer");
            return;
        }

        string svg = GenerateSvg(num);
        if (svg == null)
        {
            await Error(response, 404, "Unknown figure code");
            return;
        }

        byte[] buffer = Encoding.UTF8.GetBytes(svg);
        response.StatusCode = 200;
        response.ContentType = "image/svg+xml; charset=utf-8";
        response.ContentLength64 = buffer.LongLength;
        await response.OutputStream.WriteAsync(buffer);
        Log.Info($"Served SVG for code {num}");
    }

    private static string GenerateSvg(int num)
    {
        if (num == 1)
        {
            return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\" viewBox=\"0 0 200 200\"><circle cx=\"100\" cy=\"100\" r=\"80\" fill=\"red\" stroke=\"red\" stroke-width=\"4\"/></svg>";
        }
        if (num == 2)
        {
            return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\" viewBox=\"0 0 200 200\"><rect x=\"20\" y=\"20\" width=\"160\" height=\"160\" fill=\"red\" stroke=\"red\" stroke-width=\"4\"/></svg>";
        }
        if (num == 3)
        {
            return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\" viewBox=\"0 0 200 200\"><polygon points=\"100,20 180,180 20,180\" fill=\"red\" stroke=\"red\" stroke-width=\"4\"/></svg>";
        }
        if (num == 4)
        {
            return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\" viewBox=\"0 0 200 200\"><polygon points=\"100,20 120,75 178,75 132,110 148,168 100,138 52,168 68,110 22,75 80,75\" fill=\"red\" stroke=\"red\" stroke-width=\"4\"/></svg>";
        }
        return "<div>Figure not found =(</div>";
    }

    private async Task Error(HttpListenerResponse response, int statusCode, string message)
    {
        response.StatusCode = statusCode;
        string errorHtml = $$"""
            <html>
                <head><title>Error {{statusCode}}</title></head>
                <body>
                    <h1>Error {{statusCode}}</h1>
                    <p>{{message}}</p>
                </body>
            </html>
            """;
        
        byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        
        Log.Error($"{statusCode}: {message}");
    }
}
