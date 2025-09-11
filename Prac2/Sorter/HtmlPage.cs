using System.Net;
using System.Text;

namespace Sorter;

public static class HtmlPage
{
    public static string BuildHome(string baseUrl)
    {
        string sample1 = baseUrl + "sort?array=5,1,9,2,7,3";
        string sample2 = baseUrl + "sort?array=10,9,8,7,6,5,4,3,2,1";
        StringBuilder sb = new StringBuilder();
        sb.Append("""
<!doctype html>
<html lang="ru">
<head>
<meta charset="utf-8">
<title>Sorter — сортировка слиянием</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
:root{--gap:18px;--radius:14px;--border:#e5e7eb;--muted:#6b7280}
*{box-sizing:border-box}
body{font-family:system-ui,-apple-system,"Segoe UI",Roboto,Arial,sans-serif;line-height:1.5;margin:24px}
.card{border:1px solid var(--border);border-radius:var(--radius);padding:16px;background:#fff;max-width:920px}
.controls{display:flex;gap:10px;margin-top:10px;flex-wrap:wrap}
input[type=text]{flex:1;min-width:240px;padding:10px 12px;border:1px solid var(--border);border-radius:12px;font:inherit}
button,.btn{appearance:none;border:1px solid var(--border);background:#f8fafc;padding:10px 14px;border-radius:12px;text-decoration:none;color:inherit;display:inline-flex;align-items:center;justify-content:center;cursor:pointer}
small{color:var(--muted)}
h1{margin:0 0 12px 0}
h2{margin:20px 0 12px 0}
</style>
</head>
<body>
<h1>Sorter</h1>
<div class="card">
<h2>Сортировка слиянием</h2>
<form action="sort" method="get" onsubmit="return true">
<label for="array">Массив (через запятую)</label>
<div class="controls">
<input id="array" name="array" type="text" placeholder="5, 1, 9, 2, 7, 3" value="5, 1, 9, 2, 7, 3">
""");
        sb.Append("<a class=\"btn\" href=\"");
        sb.Append(WebUtility.HtmlEncode(sample1));
        sb.Append("\">Пример 1</a>\n");
        sb.Append("<a class=\"btn\" href=\"");
        sb.Append(WebUtility.HtmlEncode(sample2));
        sb.Append("\">Пример 2</a>\n");
        sb.Append("""
</div>
<p><small>Передайте массив как строку с числами, разделёнными запятыми. Результат вернётся на странице <code>/sort</code>.</small></p>
</form>
</div>
</body>
</html>
""");
        return sb.ToString();
    }

    public static string BuildResult(string input, string sorted)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("""
<!doctype html>
<html lang="ru">
<head>
<meta charset="utf-8">
<title>Результат сортировки</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
:root{--gap:18px;--radius:14px;--border:#e5e7eb;--muted:#6b7280}
*{box-sizing:border-box}
body{font-family:system-ui,-apple-system,"Segoe UI",Roboto,Arial,sans-serif;line-height:1.5;margin:24px}
.card{border:1px solid var(--border);border-radius:var(--radius);padding:16px;background:#fff;max-width:920px}
.row{display:flex;gap:10px;flex-wrap:wrap}
.box{flex:1 1 400px;border:1px dashed var(--border);border-radius:12px;padding:12px}
pre{white-space:pre-wrap;margin:0;font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace}
a.btn{appearance:none;border:1px solid var(--border);background:#f8fafc;padding:10px 14px;border-radius:12px;text-decoration:none;color:inherit;display:inline-flex;align-items:center;justify-content:center}
</style>
</head>
<body>
<h1>Результат сортировки</h1>
<div class="card">
<div class="row">
<div class="box"><strong>Вход</strong><pre>
""");
        sb.Append(WebUtility.HtmlEncode(input));
        sb.Append("""
</pre></div>
<div class="box"><strong>Выход</strong><pre>
""");
        sb.Append(WebUtility.HtmlEncode(sorted));
        sb.Append("""
</pre></div>
</div>
<p><a class="btn" href="/">Назад</a></p>
</div>
</body>
</html>
""");
        return sb.ToString();
    }
}
