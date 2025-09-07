using System.Globalization;
using System.Net;
using System.Text;
using Utils;

namespace Sorter.Features;

public class WebServer
{
    private readonly string _wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

    public async Task StartAsync(string url)
    {
        try
        {
            if (!url.EndsWith('/'))
            {
                url += "/";
            }

            if (!Directory.Exists(_wwwRoot))
            {
                Directory.CreateDirectory(_wwwRoot);
            }

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();

            Log.Info("Server started at " + url);

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
                Log.Error(ex.Message);
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        try
        {
            if (request.HttpMethod == "GET")
            {
                await GetRequest(request, response);
            }
            else
            {
                await Error(response, 405, "Method not supported");
            }
        }
        catch (Exception e)
        {
            await Error(response, 500, e.Message);
        }
        finally
        {
            response.Close();
        }
    }

    private async Task GetRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url == null ? "/" : request.Url.AbsolutePath;

        if (path == "/")
        {
            await HandleHome(request, response);
            return;
        }

        if (string.Equals(path, "/sort", StringComparison.OrdinalIgnoreCase))
        {
            await HandleSort(request, response);
            return;
        }

        await TryServeStatic(path, response);
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

    private async Task HandleSort(HttpListenerRequest request, HttpListenerResponse response)
    {
        string src = request.QueryString["array"];
        if (string.IsNullOrWhiteSpace(src))
        {
            await Error(response, 400, "Query parameter 'array' is required");
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
                await Error(response, 400, "All elements must be integers");
                return;
            }
            numbers[i] = val;
        }

        int[] sorted = MergeSorter.Sort(numbers);
        string result = string.Join(", ", sorted.Select(x => x.ToString(CultureInfo.InvariantCulture)));

        string html = HtmlPage.BuildResult(src, result);
        byte[] buffer = Encoding.UTF8.GetBytes(html);
        response.StatusCode = 200;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = buffer.LongLength;
        await response.OutputStream.WriteAsync(buffer);
        Log.Info("Sorted " + numbers.Length.ToString(CultureInfo.InvariantCulture) + " numbers");
    }

    private async Task TryServeStatic(string path, HttpListenerResponse response)
    {
        string filePath = Path.Combine(_wwwRoot, path.TrimStart('/'));
        string full = Path.GetFullPath(filePath);

        if (!full.StartsWith(_wwwRoot, StringComparison.OrdinalIgnoreCase))
        {
            await Error(response, 403, "Access denied");
            return;
        }

        if (!File.Exists(full))
        {
            await Error(response, 404, "Not found");
            return;
        }

        byte[] content = await File.ReadAllBytesAsync(full);
        string mime = ContentTypes.GetMimeType(full);
        response.ContentType = NeedsUtf8(mime) ? mime + "; charset=utf-8" : mime;
        response.ContentLength64 = content.LongLength;
        response.StatusCode = 200;
        await response.OutputStream.WriteAsync(content);
        Log.Info("Served file: " + full);
    }

    private static bool NeedsUtf8(string mime)
    {
        if (mime.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(mime, "application/javascript", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(mime, "application/json", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(mime, "image/svg+xml", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private async Task Error(HttpListenerResponse response, int statusCode, string message)
    {
        response.StatusCode = statusCode;
        string errorHtml =
            "<html>" +
            "<head><meta charset=\"utf-8\"><title>Error " + statusCode + "</title></head>" +
            "<body style=\"font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif;\">" +
            "<h1>Error " + statusCode + "</h1>" +
            "<p>" + WebUtility.HtmlEncode(message) + "</p>" +
            "</body>" +
            "</html>";
        byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        Log.Error(statusCode + ": " + message);
    }
}
