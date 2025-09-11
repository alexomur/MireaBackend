using System;
using System.Threading.Tasks;
using AdminPanel.Features;

namespace AdminPanel;

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
        string? envUrl = Environment.GetEnvironmentVariable("ADMINPANEL_URL");
        if (!string.IsNullOrWhiteSpace(envUrl)) return envUrl;
        string? portStr = Environment.GetEnvironmentVariable("PORT");
        if (string.IsNullOrWhiteSpace(portStr)) portStr = Environment.GetEnvironmentVariable("ADMINPANEL_PORT");
        int port = 8082;
        if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out int p) && p > 0) port = p;
        return $"http://0.0.0.0:{port}";
    }
}