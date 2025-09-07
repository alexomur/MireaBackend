using System.Threading.Tasks;
using AdminPanel.Features;

namespace AdminPanel;

public static class Program
{
    public static async Task Main(string[] args)
    {
        WebServer server = new WebServer();
        await server.StartAsync("http://127.0.0.1:8080");
    }
}