using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace AdminPanel;

public static class HtmlPage
{
    public static string BuildHome(string baseUrl, IEnumerable<string> keys)
    {
        string execBase = baseUrl + "exec?key=";
        StringBuilder sb = new StringBuilder();
        sb.Append("<!doctype html>\n");
        sb.Append("<html lang=\"ru\">\n<head>\n<meta charset=\"utf-8\">\n<title>AdminPanel</title>\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<style>\n");
        sb.Append(":root{--radius:14px;--border:#e5e7eb;--muted:#6b7280}\n*{box-sizing:border-box}\nbody{font-family:system-ui,-apple-system,\"Segoe UI\",Roboto,Arial,sans-serif;margin:24px;line-height:1.5}\n");
        sb.Append(".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:12px}\n.card{border:1px solid var(--border);border-radius:var(--radius);padding:12px;background:#fff}\n");
        sb.Append(".btn{appearance:none;border:1px solid var(--border);background:#f8fafc;padding:8px 12px;border-radius:10px;text-decoration:none;color:inherit;display:inline-flex;align-items:center;justify-content:center;cursor:pointer}\n");
        sb.Append("input{font:inherit;border:1px solid var(--border);border-radius:10px;padding:8px 10px}\nlabel{display:flex;gap:8px;align-items:center}\nh1{margin:0 0 10px 0}\nsmall{color:var(--muted)}\npre{white-space:pre-wrap}\n");
        sb.Append("</style>\n</head>\n<body>\n<h1>AdminPanel</h1>\n");
        sb.Append("<p><small>Доступные команды: whoami, id, uname -a, uptime, df -h, free -m, ps aux, ls -la, env, pwd, hostname.</small></p>\n");
        sb.Append("<h2>Быстрый запуск</h2>\n<div class=\"grid\">\n");
        foreach (string k in keys.OrderBy(x => x))
        {
            if (k == "ls") continue;
            string href = execBase + WebUtility.UrlEncode(k);
            sb.Append("<div class=\"card\"><div style=\"display:flex;justify-content:space-between;gap:8px;align-items:center\">");
            sb.Append("<div><strong>");
            sb.Append(WebUtility.HtmlEncode(k));
            sb.Append("</strong></div>");
            sb.Append("<a class=\"btn\" href=\"");
            sb.Append(WebUtility.HtmlEncode(href));
            sb.Append("\">Выполнить</a></div></div>\n");
        }
        sb.Append("</div>\n<h2>ls по пути</h2>\n<div class=\"card\">\n<form action=\"exec\" method=\"get\">\n");
        sb.Append("<input type=\"hidden\" name=\"key\" value=\"ls\">\n<label>Путь <input type=\"text\" name=\"path\" placeholder=\".\" value=\".\"></label>\n<button class=\"btn\" type=\"submit\">Выполнить</button>\n</form>\n");
        sb.Append("<p><small>Путь фильтруется по допустимым символам: A–Z a–z 0–9 _ - . / ~ и пробел.</small></p>\n</div>\n");
        sb.Append("<h2>Псевдо-SSH</h2>\n<div class=\"card\">\n<form action=\"SSH\" method=\"get\">\n");
        sb.Append("<label>Команда <input type=\"text\" name=\"q\" placeholder=\"ls .\"></label>\n");
        sb.Append("<label>Token <input type=\"text\" name=\"token\" placeholder=\"если задан ADMINPANEL_TOKEN\"></label>\n");
        sb.Append("<button class=\"btn\" type=\"submit\">Выполнить</button>\n</form>\n");
        sb.Append("<p><small>Разрешён ограниченный набор команд. На Windows используется PowerSSH-эквивалент.</small></p>\n</div>\n");
        sb.Append("</body>\n</html>\n");
        return sb.ToString();
    }

    public static string BuildResult(string title, string command, string stdout, string stderr, int exitCode, long elapsedMs)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<!doctype html>\n<html lang=\"ru\">\n<head>\n<meta charset=\"utf-8\">\n<title>Результат команды</title>\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<style>\n");
        sb.Append(":root{--radius:14px;--border:#e5e7eb;--muted:#6b7280}\n*{box-sizing:border-box}\nbody{font-family:system-ui,-apple-system,\"Segoe UI\",Roboto,Arial,sans-serif;margin:24px;line-height:1.5}\n");
        sb.Append(".card{border:1px solid var(--border);border-radius:var(--radius);padding:16px;background:#fff}\npre{white-space:pre-wrap;margin:0;font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace}\n");
        sb.Append(".badge{display:inline-block;border:1px solid var(--border);border-radius:999px;padding:4px 10px;margin-right:8px}\n");
        sb.Append(".btn{appearance:none;border:1px solid var(--border);background:#f8fafc;padding:8px 12px;border-radius:10px;text-decoration:none;color:inherit;display:inline-flex;align-items:center;justify-content:center;cursor:pointer}\n");
        sb.Append("</style>\n</head>\n<body>\n<h1>Результат</h1>\n<div class=\"card\">\n");
        sb.Append("<div><span class=\"badge\">exit " + exitCode.ToString(CultureInfo.InvariantCulture) + "</span><span class=\"badge\">" + elapsedMs.ToString(CultureInfo.InvariantCulture) + " ms</span></div>\n");
        sb.Append("<div><strong>Название</strong><div>" + WebUtility.HtmlEncode(title ?? "") + "</div></div>\n");
        sb.Append("<div><strong>Команда</strong><div><code>" + WebUtility.HtmlEncode(command ?? "") + "</code></div></div>\n");
        sb.Append("<div><strong>Stdout</strong><pre>" + WebUtility.HtmlEncode(stdout ?? "") + "</pre></div>\n");
        sb.Append("<div><strong>Stderr</strong><pre>" + WebUtility.HtmlEncode(stderr ?? "") + "</pre></div>\n");
        sb.Append("<p><a class=\"btn\" href=\"/\">Назад</a></p>\n</div>\n</body>\n</html>\n");
        return sb.ToString();
    }

    public static string BuildShell(string baseUrl, string q, string title, string command, string stderr, int exitCode, long elapsedMs, string stdout)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<!doctype html>\n<html lang=\"ru\">\n<head>\n<meta charset=\"utf-8\">\n<title>SSH</title>\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<style>\n");
        sb.Append(":root{--radius:14px;--border:#e5e7eb;--muted:#6b7280}\n*{box-sizing:border-box}\nbody{font-family:system-ui,-apple-system,\"Segoe UI\",Roboto,Arial,sans-serif;margin:24px;line-height:1.5}\n");
        sb.Append(".card{border:1px solid var(--border);border-radius:var(--radius);padding:16px;background:#fff}\npre{white-space:pre-wrap;margin:0;font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace}\n");
        sb.Append(".row{display:grid;grid-template-columns:1fr;gap:12px}\n");
        sb.Append(".btn{appearance:none;border:1px solid var(--border);background:#f8fafc;padding:8px 12px;border-radius:10px;text-decoration:none;color:inherit;display:inline-flex;align-items:center;justify-content:center;cursor:pointer}\n");
        sb.Append("label{display:flex;gap:8px;align-items:center}\ninput{font:inherit;border:1px solid var(--border);border-radius:10px;padding:8px 10px}\n");
        sb.Append("</style>\n</head>\n<body>\n<h1>Псевдо-SSH</h1>\n<div class=\"card\">\n<form action=\"/SSH\" method=\"get\">\n");
        sb.Append("<label>Команда <input type=\"text\" name=\"q\" value=\"" + WebUtility.HtmlEncode(q ?? "") + "\" placeholder=\"ls .\"></label>\n");
        sb.Append("<label>Token <input type=\"text\" name=\"token\" placeholder=\"если задан ADMINPANEL_TOKEN\"></label>\n");
        sb.Append("<button class=\"btn\" type=\"submit\">Выполнить</button> <a class=\"btn\" href=\"/\">На главную</a>\n</form>\n</div>\n");
        if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(command) || exitCode >= 0)
        {
            sb.Append("<div class=\"card\" style=\"margin-top:12px\">\n<div class=\"row\">\n");
            if (exitCode >= 0) sb.Append("<div><strong>Статус</strong><div>exit " + exitCode.ToString(CultureInfo.InvariantCulture) + ", " + elapsedMs.ToString(CultureInfo.InvariantCulture) + " ms</div></div>\n");
            if (!string.IsNullOrEmpty(title)) sb.Append("<div><strong>Название</strong><div>" + WebUtility.HtmlEncode(title) + "</div></div>\n");
            if (!string.IsNullOrEmpty(command)) sb.Append("<div><strong>Команда</strong><div><code>" + WebUtility.HtmlEncode(command) + "</code></div></div>\n");
            if (!string.IsNullOrEmpty(stdout)) sb.Append("<div><strong>Stdout</strong><pre>" + WebUtility.HtmlEncode(stdout) + "</pre></div>\n");
            if (!string.IsNullOrEmpty(stderr)) sb.Append("<div><strong>Stderr</strong><pre>" + WebUtility.HtmlEncode(stderr) + "</pre></div>\n");
            sb.Append("</div>\n</div>\n");
        }
        sb.Append("</body>\n</html>\n");
        return sb.ToString();
    }
}
