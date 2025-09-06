using System.Globalization;
using System.Net;
using System.Text;
using Drawer.Enums;
using Utils;

namespace Drawer.Features;

public class WebServer
{
    private readonly string _wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

    public async Task StartAsync(string url)
    {
        try
        {
            Log.Info("\n===\nSTARTING WEBSERVER\n===");
            if (!url.EndsWith('/'))
            {
                url += "/";
                Log.Warn("URL changed to: " + url);
            }

            if (!Directory.Exists(_wwwRoot))
            {
                Directory.CreateDirectory(_wwwRoot);
            }

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();

            Log.Info("Server started at " + url);
            Log.Info("Root dir: " + _wwwRoot);

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
                Log.Error("Server error: " + ex.Message);
            }
        }
        catch (Exception e)
        {
            Log.Error(e + "\n" + e.Message);
        }
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        try
        {
            string remote = request.RemoteEndPoint != null ? request.RemoteEndPoint.ToString() : "";
            string url = request.Url != null ? request.Url.ToString() : "";
            Log.Info(request.HttpMethod + " " + url + " from " + remote);

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
                Log.Warn("Method " + request.HttpMethod + " not supported");
                await Error(response, 405, "Method not supported");
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message + "\n" + e.StackTrace);
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
            await HandleHome(request, response);
            return;
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
            Log.Info("Served file: " + filePath);
        }
        else
        {
            await Error(response, 404, "File not found");
        }
    }

    private async Task HandleHome(HttpListenerRequest request, HttpListenerResponse response)
    {
        string baseUrl = request.Url == null ? "http://localhost:8080/" : request.Url.GetLeftPart(UriPartial.Authority) + "/";
        string html = HtmlPage.BuildHome(baseUrl);
        byte[] buffer = Encoding.UTF8.GetBytes(html);
        response.StatusCode = 200;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = buffer.LongLength;
        await response.OutputStream.WriteAsync(buffer);
        Log.Info("Served homepage");
    }

    private static bool TryGetInt(System.Collections.Specialized.NameValueCollection qs, string name, int defaultValue, out int value)
    {
        string raw = qs[name];
        if (string.IsNullOrEmpty(raw))
        {
            value = defaultValue;
            return true;
        }
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private async Task HandleDrawer(HttpListenerRequest request, HttpListenerResponse response)
    {
        int shapeRaw;
        bool okShape = TryGetInt(request.QueryString, "shape", 0, out shapeRaw);
        if (!okShape)
        {
            await Error(response, 400, "Parameter 'shape' must be an integer");
            return;
        }

        ShapeType shape = shapeRaw == 1 ? ShapeType.Circle :
                          shapeRaw == 2 ? ShapeType.Rectangle :
                          shapeRaw == 3 ? ShapeType.Triangle :
                          shapeRaw == 4 ? ShapeType.Star : ShapeType.None;

        if (shape == ShapeType.None)
        {
            await Error(response, 400, "Unknown shape");
            return;
        }

        int colorIndex;
        bool okColor = TryGetInt(request.QueryString, "color", 1, out colorIndex);
        if (!okColor)
        {
            await Error(response, 400, "Parameter 'color' must be an integer");
            return;
        }

        int width;
        bool okW = TryGetInt(request.QueryString, "width", 200, out width);
        if (!okW || width <= 0)
        {
            await Error(response, 400, "Parameter 'width' must be a positive integer");
            return;
        }

        int height;
        bool okH = TryGetInt(request.QueryString, "height", 200, out height);
        if (!okH || height <= 0)
        {
            await Error(response, 400, "Parameter 'height' must be a positive integer");
            return;
        }

        int stroke;
        bool okS = TryGetInt(request.QueryString, "stroke", 2, out stroke);
        if (!okS || stroke < 0)
        {
            await Error(response, 400, "Parameter 'stroke' must be a non-negative integer");
            return;
        }

        int padding;
        bool okP = TryGetInt(request.QueryString, "padding", 0, out padding);
        if (!okP || padding < 0)
        {
            await Error(response, 400, "Parameter 'padding' must be a non-negative integer");
            return;
        }

        string svg = SvgRenderer.Render(shape, colorIndex, width, height, stroke, padding);
        byte[] buffer = Encoding.UTF8.GetBytes(svg);
        response.StatusCode = 200;
        response.ContentType = "image/svg+xml; charset=utf-8";
        response.ContentLength64 = buffer.LongLength;
        await response.OutputStream.WriteAsync(buffer);
        Log.Info("Served SVG " + shape.ToString() + " " + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture));
    }

    private async Task Error(HttpListenerResponse response, int statusCode, string message)
    {
        response.StatusCode = statusCode;
        string errorHtml = $"""
            <html>
                <head><meta charset="utf-8"><title>Error {statusCode} </title></head>
                <body style="font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif;">
                    <h1>Error {statusCode}</h1>
                    <p>{message}</p>
                </body>
            </html>
            """;

        byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);

        Log.Error(statusCode + ": " + message);
    }
}
