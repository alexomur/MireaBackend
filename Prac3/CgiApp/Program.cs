using System.Net;
using System.Text;
using Npgsql;

static void WriteHeader(string status = "200 OK", string contentType = "text/html; charset=utf-8")
{
    Console.WriteLine("Status: " + status);
    Console.WriteLine("Content-Type: " + contentType);
    Console.WriteLine();
}

static string Html(string title, string body)
{
    string safe = WebUtility.HtmlEncode(title);
    StringBuilder sb = new StringBuilder();
    sb.Append("<!doctype html><html lang=\"ru\"><head><meta charset=\"utf-8\"/><title>");
    sb.Append(safe);
    sb.Append("</title><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
    sb.Append("<style>body{font-family:system-ui,Arial,sans-serif;max-width:900px;margin:2rem auto;padding:0 1rem}</style></head><body><h1>");
    sb.Append(safe);
    sb.Append("</h1>");
    sb.Append(body);
    sb.Append("<p><a href=\"/\">На главную</a></p></body></html>");
    return sb.ToString();
}

static string Env(string key)
{
    return Environment.GetEnvironmentVariable(key) ?? "";
}

static async Task<NpgsqlConnection> OpenDbAsync()
{
    string cs = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? "Host=db;Username=app;Password=secret;Database=coffee";
    NpgsqlConnection conn = new NpgsqlConnection(cs);
    await conn.OpenAsync();
    return conn;
}

static async Task<IDictionary<string, string>> ReadFormUrlEncodedAsync()
{
    string lenStr = Env("CONTENT_LENGTH");
    int length;
    if (!int.TryParse(lenStr, out length) || length <= 0) return new Dictionary<string, string>();
    Stream input = Console.OpenStandardInput();
    MemoryStream ms = new MemoryStream();
    byte[] buf = new byte[8192];
    int remaining = length;
    while (remaining > 0)
    {
        int take = remaining < buf.Length ? remaining : buf.Length;
        int read = input.Read(buf, 0, take);
        if (read <= 0) break;
        ms.Write(buf, 0, read);
        remaining -= read;
    }
    string s = Encoding.UTF8.GetString(ms.ToArray());
    Dictionary<string, string> dict = new Dictionary<string, string>();
    foreach (string p in s.Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        string[] kv = p.Split('=', 2);
        string k = WebUtility.UrlDecode(kv[0]);
        string val = kv.Length > 1 ? WebUtility.UrlDecode(kv[1]) ?? "" : "";
        if (k != null) dict[k] = val;
    }
    return dict;
}

string method = Env("REQUEST_METHOD").ToUpperInvariant();
string pathInfo = Env("PATH_INFO");
string uri = Env("REQUEST_URI");

try
{
    if (method == "GET" && string.Equals(pathInfo, "/menu", StringComparison.OrdinalIgnoreCase))
    {
        using (NpgsqlConnection db = await OpenDbAsync())
        using (NpgsqlCommand cmd = new NpgsqlCommand("select id, name, price_cents from coffee order by id", db))
        using (NpgsqlDataReader rd = await cmd.ExecuteReaderAsync())
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<table border='1' cellpadding='6'><tr><th>ID</th><th>Напиток</th><th>Цена</th></tr>");
            while (await rd.ReadAsync())
            {
                int id = rd.GetInt32(0);
                string name = WebUtility.HtmlEncode(rd.GetString(1));
                decimal price = rd.GetInt32(2) / 100.0M;
                sb.Append("<tr><td>");
                sb.Append(id);
                sb.Append("</td><td>");
                sb.Append(name);
                sb.Append("</td><td>");
                sb.Append(price.ToString("F2"));
                sb.Append(" ₽</td></tr>");
            }
            sb.Append("</table>");
            sb.Append("<h2>Сделать заказ</h2><form method=\"post\" action=\"/order\">");
            sb.Append("<label>Имя: <input name=\"customer_name\" required></label><br/>");
            sb.Append("<label>ID кофе: <input name=\"coffee_id\" type=\"number\" required></label><br/>");
            sb.Append("<label>Кол-во: <input name=\"qty\" type=\"number\" min=\"1\" value=\"1\" required></label><br/>");
            sb.Append("<button type=\"submit\">Отправить</button></form>");
            WriteHeader();
            Console.Write(Html("Меню кофейни", sb.ToString()));
        }
    }
    else if (method == "POST" && string.Equals(pathInfo, "/order", StringComparison.OrdinalIgnoreCase))
    {
        IDictionary<string, string> form = await ReadFormUrlEncodedAsync();
        string customer;
        string coffeeIdStr;
        string qtyStr;
        if (!form.TryGetValue("customer_name", out customer) ||
            !form.TryGetValue("coffee_id", out coffeeIdStr) ||
            !form.TryGetValue("qty", out qtyStr))
        {
            WriteHeader("400 Bad Request");
            Console.Write(Html("Ошибка", "<p>Некорректные данные заказа.</p>"));
        }
        else
        {
            int coffeeIdParsed;
            int qtyParsed;
            if (!int.TryParse(coffeeIdStr, out coffeeIdParsed) || !int.TryParse(qtyStr, out qtyParsed) || qtyParsed <= 0)
            {
                WriteHeader("400 Bad Request");
                Console.Write(Html("Ошибка", "<p>Некорректные данные заказа.</p>"));
            }
            else
            {
                using (NpgsqlConnection db = await OpenDbAsync())
                using (NpgsqlCommand cmd = new NpgsqlCommand("insert into orders (customer_name, coffee_id, qty) values (@c, @id, @q)", db))
                {
                    cmd.Parameters.AddWithValue("c", customer);
                    cmd.Parameters.AddWithValue("id", coffeeIdParsed);
                    cmd.Parameters.AddWithValue("q", qtyParsed);
                    await cmd.ExecuteNonQueryAsync();
                }
                WriteHeader();
                Console.Write(Html("Заказ принят", "<p>Спасибо! Заказ записан.</p><p><a href=\"/menu\">Назад к меню</a></p>"));
            }
        }
    }
    else if (method == "GET" && string.Equals(pathInfo, "/orders", StringComparison.OrdinalIgnoreCase))
    {
        using (NpgsqlConnection db = await OpenDbAsync())
        using (NpgsqlCommand cmd = new("select o.id, o.customer_name, c.name, o.qty, o.created_at from orders o join coffee c on c.id = o.coffee_id order by o.created_at desc limit 50", db))
        using (NpgsqlDataReader rd = await cmd.ExecuteReaderAsync())
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<table border='1' cellpadding='6'><tr><th>#</th><th>Клиент</th><th>Напиток</th><th>Кол-во</th><th>Когда</th></tr>");
            while (await rd.ReadAsync())
            {
                int id = rd.GetInt32(0);
                string customer = WebUtility.HtmlEncode(rd.GetString(1));
                string coffee = WebUtility.HtmlEncode(rd.GetString(2));
                int qty = rd.GetInt32(3);
                DateTime ts = rd.GetDateTime(4);
                sb.Append("<tr><td>");
                sb.Append(id);
                sb.Append("</td><td>");
                sb.Append(customer);
                sb.Append("</td><td>");
                sb.Append(coffee);
                sb.Append("</td><td>");
                sb.Append(qty);
                sb.Append("</td><td>");
                sb.Append(ts.ToString("yyyy-MM-dd HH:mm"));
                sb.Append("</td></tr>");
            }
            sb.Append("</table>");
            WriteHeader();
            Console.Write(Html("Админ: последние заказы", sb.ToString()));
        }
    }
    else
    {
        WriteHeader("404 Not Found");
        string b = "<p>Метод: " + method + "<br/>URI: " + WebUtility.HtmlEncode(uri) + "<br/>PATH_INFO: " + WebUtility.HtmlEncode(pathInfo) + "</p>";
        Console.Write(Html("Не найдено", b));
    }
}
catch (Exception ex)
{
    WriteHeader("500 Internal Server Error");
    Console.Write(Html("Ошибка сервера", "<pre>" + WebUtility.HtmlEncode(ex.ToString()) + "</pre>"));
}
