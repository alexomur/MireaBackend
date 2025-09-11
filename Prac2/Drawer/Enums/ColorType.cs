namespace Drawer.Enums;

public enum ColorType
{
    Black = 0,
    Red = 1,
    Green = 2,
    Blue = 3,
    Yellow = 4,
    DarkPink = 5,
    Cyan = 6,
    Orange = 7,
    Purple = 8,
    Pink = 9,
    Brown = 10,
    DarkCyan = 11,
    DarkBlue = 12,
    DarkGreen = 13,
    DarkGray = 14,
    Gray = 15
}

public static class ColorTypeExtensions
{
    private static readonly string[] Palette =
    [
        "#000000", // Black
        "#ff0000", // Red
        "#00aa00", // Green
        "#0066ff", // Blue
        "#ffcc00", // Yellow
        "#ff00aa", // DarkPink
        "#00cccc", // Cyan
        "#ff6600", // Orange
        "#7a3fff", // Purple
        "#ff66cc", // Pink
        "#8b4513", // Brown
        "#008080", // DarkCyan
        "#001f3f", // DarkBlue
        "#808000", // DarkGreen
        "#808080", // DarkGray
        "#c0c0c0"  // Gray
    ];

    private static readonly Dictionary<ColorType, string> ColorNames = new()
    {
        [ColorType.Black] = "Чёрный",
        [ColorType.Red] = "Красный",
        [ColorType.Green] = "Зелёный",
        [ColorType.Blue] = "Синий",
        [ColorType.Yellow] = "Жёлтый",
        [ColorType.DarkPink] = "Тёмно-розовый",
        [ColorType.Cyan] = "Голубой",
        [ColorType.Orange] = "Оранжевый",
        [ColorType.Purple] = "Фиолетовый",
        [ColorType.Pink] = "Розовый",
        [ColorType.Brown] = "Коричневый",
        [ColorType.DarkCyan] = "Тёмно-голубой",
        [ColorType.DarkBlue] = "Тёмно-синий",
        [ColorType.DarkGreen] = "Тёмно-зелёный",
        [ColorType.DarkGray] = "Тёмно-серый",
        [ColorType.Gray] = "Серый"
    };

    public static string ToHex(this ColorType colorType)
    {
        int index = (int)colorType;
        if (index >= 0 && index < Palette.Length)
            return Palette[index];
        return "#000000";
    }

    public static string ToRussianName(this ColorType colorType)
    {
        return ColorNames.GetValueOrDefault(colorType, "Неизвестный");
    }
}
