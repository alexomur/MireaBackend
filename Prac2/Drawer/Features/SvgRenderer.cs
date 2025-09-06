using System.Globalization;
using System.Text;
using Drawer.Enums;

namespace Drawer.Features;

public static class SvgRenderer
{
    private static readonly string[] Palette =
    [
        "#000000",
        "#ff0000",
        "#00aa00",
        "#0066ff",
        "#ffcc00",
        "#ff00aa",
        "#00cccc",
        "#ff6600",
        "#7a3fff",
        "#ff66cc",
        "#8b4513",
        "#008080",
        "#001f3f",
        "#808000",
        "#808080",
        "#c0c0c0"
    ];

    public static string Render(ShapeType shape, int colorId, int w, int h, int stroke, int pad)
    {
        double padX = w * Math.Clamp(pad, 0, 30) / 100.0;
        double padY = h * Math.Clamp(pad, 0, 30) / 100.0;
        double innerW = Math.Max(0, w - 2 * padX);
        double innerH = Math.Max(0, h - 2 * padY);
        string color = Palette[colorId];
        string strokeAttr = stroke > 0 ? $" stroke=\"{color}\" stroke-width=\"{stroke.ToString(CultureInfo.InvariantCulture)}\"" : "";
        string open = $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {w} {h}\">";
        if (shape == ShapeType.Circle)
        {
            double r = Math.Min(innerW, innerH) / 2.0;
            double cx = w / 2.0, cy = h / 2.0;
            return FormattableString.Invariant($"""
                {open}<circle cx="{cx:0.###}" cy="{cy:0.###}" r="{r:0.###}" fill="{color}"{strokeAttr}/></svg>
                """);
        }
        if (shape == ShapeType.Rectangle)
        {
            return FormattableString.Invariant($"""
                {open}<rect x="{padX:0.###}" y="{padY:0.###}" width="{innerW:0.###}" height="{innerH:0.###}" fill="{color}"{strokeAttr}/></svg>
                """);
        }
        if (shape == ShapeType.Triangle)
        {
            double s = Math.Min(innerW, innerH);
            double cx = w / 2.0, cy = h / 2.0, hh = s * Math.Sqrt(3) / 2.0;
            double yTop = cy - hh / 2.0, yBottom = cy + hh / 2.0, xL = cx - s / 2.0, xR = cx + s / 2.0;
            return FormattableString.Invariant($"""
                {open}<polygon points="{xL:0.###},{yBottom:0.###} {xR:0.###},{yBottom:0.###} {cx:0.###},{yTop:0.###}" fill="{color}"{strokeAttr}/></svg>
                """);
        }
        if (shape == ShapeType.Star)
        {
            double s = Math.Min(innerW, innerH);
            double cx = w / 2.0, cy = h / 2.0, outerR = s / 2.0, innerR = outerR * 0.5;
            string pts = BuildStar(cx, cy, outerR, innerR, 5);
            return $"{open}<polygon points=\"{pts}\" fill=\"{color}\"{strokeAttr}/></svg>";
        }
        return $"{open}<text x=\"10\" y=\"20\">Unknown</text></svg>";
    }

    private static string BuildStar(double cx, double cy, double R, double r, int n)
    {
        StringBuilder b = new();
        double rot = -Math.PI / 2.0, step = Math.PI / n;
        for (int i = 0; i < n; i++)
        {
            double x1 = cx + Math.Cos(rot) * R, y1 = cy + Math.Sin(rot) * R; rot += step;
            double x2 = cx + Math.Cos(rot) * r, y2 = cy + Math.Sin(rot) * r; rot += step;
            b.Append(CultureInfo.InvariantCulture, $"{x1:0.###},{y1:0.###} {x2:0.###},{y2:0.###} ");
        }
        return b.ToString().TrimEnd();
    }
}
