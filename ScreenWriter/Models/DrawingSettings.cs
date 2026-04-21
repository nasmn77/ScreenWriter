using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace ScreenWriter.Models;

public enum DrawingTool { Pen, Arrow, Line, Rectangle, Ellipse, Text, Eraser }

public static class DrawingSettings
{
    public static readonly (Color Color, string Tooltip)[] Palette =
    [
        (Color.FromRgb(230, 57,  70),  "أحمر"),
        (Color.FromRgb(244, 162, 97),  "برتقالي"),
        (Color.FromRgb(255, 209, 102), "أصفر"),
        (Color.FromRgb(6,   214, 160), "أخضر"),
        (Color.FromRgb(17,  138, 178), "أزرق"),
        (Color.FromRgb(131, 56,  236), "بنفسجي"),
        (Colors.White,                 "أبيض"),
        (Colors.Black,                 "أسود"),
    ];

    public const double DefaultPenSize = 4.0;
    public const double MinPenSize = 2.0;
    public const double MaxPenSize = 30.0;
}
