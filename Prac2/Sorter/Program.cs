using System.Threading.Tasks;
using Sorter.Features;

namespace Sorter;

public static class Program
{
    public static async Task Main(string[] args)
    {
        WebServer server = new WebServer();
        await server.StartAsync("http://localhost:8080/");
    }
}