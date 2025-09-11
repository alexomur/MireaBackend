using System;
using System.Threading.Tasks;
using Sorter.Features;

namespace Sorter;

public static class Program
{
    public static async Task Main(string[] args)
    {
        string url = ResolveUrl();
        WebServer server = new WebServer();
        await server.StartAsync(url);
    }

    private static string ResolveUrl()
    {
        string? envUrl = Environment.GetEnvironmentVariable("SORTER_URL");
        if (!string.IsNullOrWhiteSpace(envUrl)) return EnsureTrailingSlash(envUrl);
        string? portStr = Environment.GetEnvironmentVariable("SORTER_PORT");
        if (string.IsNullOrWhiteSpace(portStr)) portStr = Environment.GetEnvironmentVariable("PORT");
        int port = 8081;
        if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out int p) && p > 0) port = p;
        return $"http://localhost:{port}/";
    }

    private static string EnsureTrailingSlash(string url)
    {
        return url.EndsWith("/") ? url : url + "/";
    }
}