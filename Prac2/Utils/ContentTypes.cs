namespace Utils;

public static class ContentTypes
{
    private static Dictionary<string, string> MimeTypes { get; } = new()
    {
        [".html"] = "text/html",
        [".htm"] = "text/html",
        [".css"] = "text/css",
        [".js"] = "application/javascript",
        [".json"] = "application/json",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".ico"] = "image/x-icon",
        [".svg"] = "image/svg+xml",
        [".txt"] = "text/plain"
    };

    public static string GetMimeType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        return MimeTypes.GetValueOrDefault(extension, "application/octet-stream");
    }
}