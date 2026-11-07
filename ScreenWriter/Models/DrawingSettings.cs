using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace ScreenWriter.Models;

public enum DrawingTool { Pen, Arrow, Line, Rectangle, Ellipse, Text, Eraser, Select }

/// <summary>Drawing surface: transparent overlay on the screen, or an opaque white/black board.</summary>
public enum BoardMode { Transparent, White, Black }

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

    // Same order as the toolbar color buttons:
    // red, orange, yellow, green, blue, purple, white, black.
    private static readonly string[] TransparentPalette =
        ["#E63946", "#F4A261", "#FFD166", "#06D6A0", "#118AB2", "#8338EC", "#FFFFFF", "#111111"];

    // Colors that are hard to see on white are replaced with darker ones.
    private static readonly string[] WhiteBoardPalette =
        ["#E63946", "#E76F51", "#D4A017", "#2B9348", "#118AB2", "#8338EC", "#6C757D", "#111111"];

    // Colors that are hard to see on black are replaced with lighter ones.
    private static readonly string[] BlackBoardPalette =
        ["#E63946", "#F4A261", "#FFD166", "#06D6A0", "#4CC9F0", "#B388FF", "#FFFFFF", "#ADB5BD"];

    public static string[] GetPalette(BoardMode mode) => mode switch
    {
        BoardMode.White => WhiteBoardPalette,
        BoardMode.Black => BlackBoardPalette,
        _               => TransparentPalette,
    };

    /// <summary>Pen color used the first time a board is opened.</summary>
    public static Color DefaultBoardColor(BoardMode mode) => mode switch
    {
        BoardMode.White => Color.FromRgb(0x11, 0x11, 0x11),
        BoardMode.Black => Colors.White,
        _               => Color.FromRgb(230, 57, 70),
    };

    public const double DefaultPenSize = 4.0;
    public const double MinPenSize = 2.0;
    public const double MaxPenSize = 30.0;
}
