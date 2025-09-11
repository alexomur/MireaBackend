using System.Net;
using System.Text;

namespace Drawer;

public static class HtmlPage
{
    private const string Styles = ":root{--gap:18px;--radius:14px;--border:#e5e7eb;--muted:#6b7280}*{box-sizing:border-box}body{font-family:system-ui,-apple-system,\"Segoe UI\",Roboto,Arial,sans-serif;line-height:1.5;margin:24px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:var(--gap)}.card{border:1px solid var(--border);border-radius:var(--radius);overflow:hidden;background:#fff;display:flex;flex-direction:column}.preview{display:flex;align-items:center;justify-content:center;background:#fafafa;padding:12px;border-bottom:1px solid var(--border);min-height:220px}.preview img{max-width:100%;max-height:260px;display:block}.card-body{padding:14px;display:flex;flex-direction:column;gap:10px}.title{font-weight:600}.params{color:var(--muted);font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;font-size:13px}.controls{display:flex;gap:10px;align-items:center}.btn{appearance:none;border:1px solid var(--border);background:#f8fafc;padding:8px 12px;border-radius:10px;text-decoration:none;color:inherit;display:inline-flex;align-items:center;justify-content:center}.url{flex:1;min-width:120px;border:1px solid var(--border);border-radius:10px;padding:8px 10px;font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;font-size:13px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.form{border:1px solid var(--border);border-radius:var(--radius);padding:16px}.fields{display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));gap:12px;margin-bottom:12px}label{display:flex;flex-direction:column;gap:6px}input,select{font:inherit;padding:8px 10px;border:1px solid var(--border);border-radius:10px}.small{color:var(--muted);font-size:13px}h1{margin:0 0 10px 0}h2{margin:28px 0 12px 0}";

    public static string BuildHome(string baseUrl)
    {
        string l1 = baseUrl + "drawer?shape=1&color=1&width=200&height=200&stroke=2&padding=0";
        string l2 = baseUrl + "drawer?shape=2&color=4&width=240&height=160&stroke=3&padding=2";
        string l3 = baseUrl + "drawer?shape=3&color=8&width=220&height=220&stroke=2&padding=0";
        string l4 = baseUrl + "drawer?shape=4&color=2&width=260&height=200&stroke=2&padding=3";
        StringBuilder sb = new StringBuilder();
        sb.Append("""
<!doctype html>
<html lang="ru">
<head>
<meta charset="utf-8">
<title>Drawer — простые SVG фигуры</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
""");
        sb.Append(Styles);
        sb.Append("""
</style>
</head>
<body>
<h1>Drawer</h1>

<h2>Примеры</h2>
<div class="grid">
""");
        sb.Append(Card("Круг 200×200", l1));
        sb.Append(Card("Прямоугольник 240×160", l2));
        sb.Append(Card("Треугольник 220×220", l3));
        sb.Append(Card("Звезда 260×200", l4));
        sb.Append("""
</div>

<h2>Конструктор</h2>
<div class="form">
<form onsubmit="openUrl();return false">
<div class="fields">
<label>Форма
<select name="shape" id="shape">
<option value="1">Круг</option>
<option value="2">Прямоугольник</option>
<option value="3">Треугольник</option>
<option value="4">Звезда</option>
</select>
</label>
<label>Цвет
<select id="color" name="color">
<option>0</option><option>1</option><option>2</option><option>3</option>
<option>4</option><option>5</option><option>6</option><option>7</option>
<option>8</option><option>9</option><option>10</option><option>11</option>
<option>12</option><option>13</option><option>14</option><option>15</option>
</select>
</label>
<label>Ширина(px) <input id="w" name="width" type="number" min="1" max="2000" value="220"></label>
<label>Высота(px) <input id="h" name="height" type="number" min="1" max="2000" value="180"></label>
<label>Stroke(px) <input id="st" name="stroke" type="number" min="0" max="64" value="2"></label>
<label>Padding(%) <input id="pad" name="padding" type="number" min="0" max="30" value="2"></label>
</div>
<div class="controls">
<input id="result-url" class="url" type="text" readonly>
<a id="open" class="btn" href="#" target="_blank" rel="noopener">Открыть</a>
<button class="btn" type="button" onclick="copyResult()">Копировать</button>
</div>
<p class="small">Параметры: shape 1–4, color 0–15, width/height &gt; 0, stroke ≥ 0, padding 0–30.</p>
</form>
<div class="preview" style="margin-top:12px"><img id="preview" alt="Превью"></div>
</div>

<script>
function buildQuery(){const s=document.getElementById('shape').value;const c=document.getElementById('color').value;const w=document.getElementById('w').value;const h=document.getElementById('h').value;const st=document.getElementById('st').value;const p=document.getElementById('pad').value;const params=new URLSearchParams({shape:s,color:c,width:w,height:h,stroke:st,padding:p});return "drawer?"+params.toString()}
function update(){const u=buildQuery();const full=new URL(u, location.href).href;document.getElementById('result-url').value=full;document.getElementById('open').href=u;document.getElementById('preview').src=u}
function copyResult(){const v=document.getElementById('result-url').value;navigator.clipboard&&navigator.clipboard.writeText(v)}
function openUrl(){window.open(document.getElementById('open').href, "_blank")}
document.querySelectorAll('#shape,#color,#w,#h,#st,#pad').forEach(el=>el.addEventListener('input',update));update();
</script>

</body>
</html>
""");
        return sb.ToString();
    }

    private static string Card(string title, string href)
    {
        string hrefEsc = WebUtility.HtmlEncode(href);
        string q = href.Contains("?") ? href.Substring(href.IndexOf("?") + 1) : href;
        string qEsc = WebUtility.HtmlEncode(q);
        StringBuilder sb = new StringBuilder();
        sb.Append("<div class=\"card\">");
        sb.Append("<div class=\"preview\"><img loading=\"lazy\" src=\"");
        sb.Append(hrefEsc);
        sb.Append("\" alt=\"");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("\"></div>");
        sb.Append("<div class=\"card-body\">");
        sb.Append("<div class=\"title\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</div>");
        sb.Append("<div class=\"params\"><code>");
        sb.Append(qEsc);
        sb.Append("</code></div>");
        sb.Append("<div class=\"controls\">");
        sb.Append("<input class=\"url\" type=\"text\" readonly value=\"");
        sb.Append(hrefEsc);
        sb.Append("\">");
        sb.Append("<a class=\"btn\" href=\"");
        sb.Append(hrefEsc);
        sb.Append("\" target=\"_blank\" rel=\"noopener\">Открыть</a>");
        sb.Append("<button class=\"btn\" type=\"button\" onclick=\"navigator.clipboard&&navigator.clipboard.writeText(this.previousElementSibling.value)\">Копировать</button>");
        sb.Append("</div>");
        sb.Append("</div>");
        sb.Append("</div>");
        return sb.ToString();
    }
}
