using Drawer.Features;

namespace Drawer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        WebServer server = new();
        await server.StartAsync("http://localhost:8080/");
    }
}
