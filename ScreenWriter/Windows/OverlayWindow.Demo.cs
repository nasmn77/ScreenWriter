#if DEBUG
// Demo scenes for Store screenshots. Debug builds only, never shipped.
// Run with SCREENWRITER_DEMO_SCENE=<scene> (and SCREENWRITER_DEMO_LANG=ar|en).
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenWriter.Models;
using ScreenWriter.Services;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using Pen = System.Windows.Media.Pen;

namespace ScreenWriter.Windows;

public partial class OverlayWindow
{
    internal void LoadDemoScene(string scene, ToolbarWindow toolbar)
    {
        bool ar = LocalizationService.Instance.IsArabic;
        var mode = scene switch
        {
            "white" or "white-menu" or "white-file" => BoardMode.White,
            "black"                 => BoardMode.Black,
            _                       => BoardMode.Transparent,
        };

        SetBoardMode(mode);
        if (!_drawingMode) ToggleDrawingMode();

        var tool = scene == "screen-select" ? DrawingTool.Select : DrawingTool.Pen;
        toolbar.RestoreInitialTool(tool);
        toolbar.RestoreInitialPenSize(5);

        switch (scene)
        {
            case "screen":        ScreenScene(ar);         break;
            case "screen-select": ScreenSelectScene(ar);   break;
            case "white":         WhiteScene(ar);          break;
            case "white-menu":    WhiteMenuScene(ar);      break;
            case "white-file":    WhiteFileScene(ar);      break;
            case "black":         BlackScene(ar);          break;
        }

        // Pen color shown as selected in the toolbar
        _currentColor = mode switch
        {
            BoardMode.White => Hex("#111111"),
            BoardMode.Black => Hex("#FFD166"),
            _               => Hex("#E63946"),
        };
        SetDefaultAttributes();

        toolbar.SyncMode(true);
        toolbar.SyncBoardMode(mode, _currentColor);
        toolbar.PinForDemo();
        if (scene == "white-menu") toolbar.OpenBoardMenuForDemo();
        if (scene == "white-file") toolbar.OpenFileMenuForDemo();
        toolbar.BringToTopmost();
    }

    // ── Scenes ────────────────────────────────────────────────────────────────
    // Coordinates match demo-slide.html (1920×1080).

    private void ScreenScene(bool ar)
    {
        // circle the tallest bar and point at it
        DemoShape(DrawingTool.Ellipse, new(985, 255), new(1235, 405), "#E63946", 6);
        DemoShape(DrawingTool.Arrow,   new(1450, 282), new(1248, 322), "#E63946", 6);
        DemoText(ar ? "نمو 40%" : "+40% growth", new(1460, 225), "#E63946", 48, bold: true);
        // underline the title
        if (ar) DemoWave(1130, 1780, 192, "#F4A261", 6);
        else    DemoWave(140,  860,  192, "#F4A261", 6);
    }

    private void ScreenSelectScene(bool ar)
    {
        DemoShape(DrawingTool.Ellipse, new(985, 255), new(1235, 405), "#E63946", 6);
        var rect = DemoShape(DrawingTool.Rectangle, new(1448, 328), new(1812, 522), "#118AB2", 6);
        DemoShape(DrawingTool.Arrow, new(1436, 440), new(1252, 380), "#118AB2", 6);
        SelectElement(rect);
    }

    private void WhiteScene(bool ar)
    {
        const string ink = "#111111";
        if (ar) DemoText("نظرية فيثاغورس", new(1240, 110), ink, 64, bold: true);
        else    DemoText("Pythagorean theorem", new(140, 110), ink, 64, bold: true);

        // right triangle
        var A = new Point(330, 880); var B = new Point(950, 880); var C = new Point(330, 400);
        DemoShape(DrawingTool.Line, A, B, "#118AB2", 6);
        DemoShape(DrawingTool.Line, A, C, "#118AB2", 6);
        DemoShape(DrawingTool.Line, C, B, "#E63946", 6);
        DemoShape(DrawingTool.Line, new(330, 835), new(375, 835), "#118AB2", 4);
        DemoShape(DrawingTool.Line, new(375, 835), new(375, 880), "#118AB2", 4);
        DemoText("a", new(270, 600), "#118AB2", 56, bold: true);
        DemoText("b", new(620, 885), "#118AB2", 56, bold: true);
        DemoText("c", new(665, 560), "#E63946", 56, bold: true);

        // formula, circled by hand
        DemoText("a² + b² = c²", new(1160, 400), ink, 84, bold: true);
        DemoLoop(new(1430, 455), 350, 105, "#E63946", 6);
        DemoShape(DrawingTool.Arrow, new(720, 520), new(1060, 470), "#E76F51", 6);

        DemoText(ar ? "مثال:" : "Example:", ar ? new(1560, 640) : new(1160, 640), "#2B9348", 48, bold: true);
        DemoText("3² + 4² = 5²", new(1160, 720), "#2B9348", 60, bold: false);
        DemoText("9 + 16 = 25",  new(1160, 805), "#2B9348", 60, bold: false);
    }

    // Compact layout inside the top-left 1280×720 (the Store image zooms in on the menu)
    private void WhiteMenuScene(bool ar)
    {
        const string ink = "#111111";
        if (ar) DemoText("نظرية فيثاغورس", new(800, 95), ink, 54, bold: true);
        else    DemoText("Pythagorean theorem", new(540, 95), ink, 54, bold: true);

        var A = new Point(330, 650); var B = new Point(720, 650); var C = new Point(330, 330);
        DemoShape(DrawingTool.Line, A, B, "#118AB2", 6);
        DemoShape(DrawingTool.Line, A, C, "#118AB2", 6);
        DemoShape(DrawingTool.Line, C, B, "#E63946", 6);
        DemoShape(DrawingTool.Line, new(330, 615), new(365, 615), "#118AB2", 4);
        DemoShape(DrawingTool.Line, new(365, 615), new(365, 650), "#118AB2", 4);
        DemoText("a", new(280, 460), "#118AB2", 48, bold: true);
        DemoText("b", new(510, 650), "#118AB2", 48, bold: true);
        DemoText("c", new(540, 440), "#E63946", 48, bold: true);

        DemoText("a² + b² = c²", new(830, 330), ink, 60, bold: true);
        DemoLoop(new(1025, 372), 225, 75, "#E63946", 5);
        DemoText("3² + 4² = 5²", new(860, 500), "#2B9348", 46, bold: false);
    }

    // A worksheet inserted as a picture and answered with the pen (zoomed like the menu scene).
    private void WhiteFileScene(bool ar)
    {
        const double left = 430, top = 90;
        var data = BoardImage.FromBytes(BoardImage.Encode(DrawWorksheet(ar), new PngBitmapEncoder()));
        Canvas.Children.Add(CreateImage(data, left, top, data.Bitmap.PixelWidth, data.Bitmap.PixelHeight));

        double[] centers = [150, 390, 630];
        string[] answers = ["½", "¼", "¾"];
        for (int i = 0; i < 3; i++)
        {
            double x = left + centers[i];
            DemoText(answers[i], new(x - 26, top + 345), "#E63946", 62, bold: true);
            DemoStroke([new(x + 44, top + 392), new(x + 58, top + 410), new(x + 94, top + 358)], "#2B9348", 7);
        }
        DemoLoop(new(left + 630, top + 225), 112, 112, "#E63946", 5);
    }

    private static BitmapSource DrawWorksheet(bool ar)
    {
        const int w = 780, h = 520;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Hex("#FFF8E7")), new Pen(new SolidColorBrush(Hex("#D9C9A3")), 2),
                new Rect(1, 1, w - 2, h - 2), 14, 14);

            var flow = ar ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;
            void Text(string s, double size, double y, FontWeight weight)
            {
                var text = new FormattedText(s, System.Globalization.CultureInfo.InvariantCulture, flow,
                    new Typeface(new FontFamily("Arial"), FontStyles.Normal, weight, FontStretches.Normal),
                    size, new SolidColorBrush(Hex("#2B2B2B")), 1.0)
                { MaxTextWidth = w - 80, TextAlignment = TextAlignment.Center };
                dc.DrawText(text, new Point(40, y));
            }
            Text(ar ? "ورقة عمل: الكسور" : "Worksheet: Fractions", 40, 26, FontWeights.Bold);
            Text(ar ? "اكتب الكسر الذي يمثل الجزء الملوّن" : "Write the fraction for the shaded part", 26, 88, FontWeights.Normal);

            var ink  = new Pen(new SolidColorBrush(Hex("#2B2B2B")), 3);
            var fill = new SolidColorBrush(Hex("#7CC6E8"));
            double[] xs = [150, 390, 630];
            double[] parts = [0.5, 0.25, 0.75];
            const double cy = 225, r = 85;
            for (int i = 0; i < 3; i++)
            {
                var c = new Point(xs[i], cy);
                double angle = parts[i] * 2 * Math.PI;
                var end = new Point(c.X + r * Math.Sin(angle), c.Y - r * Math.Cos(angle));
                var figure = new PathFigure { StartPoint = c, IsClosed = true };
                figure.Segments.Add(new LineSegment(new Point(c.X, c.Y - r), true));
                figure.Segments.Add(new ArcSegment(end, new System.Windows.Size(r, r), 0, parts[i] > 0.5, SweepDirection.Clockwise, true));
                dc.DrawGeometry(fill, null, new PathGeometry([figure]));
                dc.DrawEllipse(null, ink, c, r, r);
                dc.DrawLine(ink, new Point(c.X - r, c.Y), new Point(c.X + r, c.Y));
                dc.DrawLine(ink, new Point(c.X, c.Y - r), new Point(c.X, c.Y + r));
                dc.DrawLine(ink, new Point(xs[i] - 55, 425), new Point(xs[i] + 40, 425));
            }
        }
        var bitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private void BlackScene(bool ar)
    {
        if (ar) DemoText("الدوال المثلثية", new(1300, 100), "#FFFFFF", 60, bold: true);
        else    DemoText("Trigonometric functions", new(140, 100), "#FFFFFF", 60, bold: true);

        // axes
        DemoShape(DrawingTool.Arrow, new(260, 620), new(1700, 620), "#FFFFFF", 4);
        DemoShape(DrawingTool.Arrow, new(320, 950), new(320, 260), "#FFFFFF", 4);
        DemoText("x", new(1700, 630), "#FFFFFF", 40, bold: false);
        DemoText("y", new(285, 230), "#FFFFFF", 40, bold: false);

        // sin and cos over two periods (x: 320 → 1520)
        DemoCurve(x => Math.Sin(x), "#FFD166", 6);
        DemoCurve(x => Math.Cos(x), "#4CC9F0", 6);

        // tick labels
        string[] ticks = ["π", "2π", "3π", "4π"];
        for (int i = 0; i < 4; i++)
        {
            double x = 320 + 300 * (i + 1);
            DemoShape(DrawingTool.Line, new(x, 605), new(x, 635), "#FFFFFF", 3);
            // odd multiples: sin falls to the right of the tick, so label on the left
            DemoText(ticks[i], new(i % 2 == 0 ? x - (i == 0 ? 34 : 54) : x + 10, 642), "#ADB5BD", 34, bold: false);
        }

        DemoText("y = sin x", new(1480, 300), "#FFD166", 46, bold: true);
        DemoText("y = cos x", new(1480, 370), "#4CC9F0", 46, bold: true);
        DemoLoop(new(920, 415), 95, 55, "#B388FF", 5);
        DemoText(ar ? "القيمة العظمى" : "maximum", new(ar ? 820 : 845, 295), "#B388FF", 38, bold: true);
    }

    // ── Builders (same objects the real tools create) ─────────────────────────
    private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private System.Windows.Shapes.Shape DemoShape(DrawingTool tool, Point a, Point b, string color, double t)
    {
        var (oldTool, oldColor, oldSize) = (_currentTool, _currentColor, _currentPenSize);
        (_currentTool, _currentColor, _currentPenSize) = (tool, Hex(color), t);
        var shape = BuildShape(a, b);
        (_currentTool, _currentColor, _currentPenSize) = (oldTool, oldColor, oldSize);
        Canvas.Children.Add(shape);
        return shape;
    }

    private void DemoText(string text, Point pos, string color, double size, bool bold)
    {
        var block = new TextBlock
        {
            Text       = text,
            Foreground = new SolidColorBrush(Hex(color)),
            FontFamily = new FontFamily("Arial"),
            FontSize   = size,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            // Arabic text reads right-to-left (punctuation on the correct side)
            FlowDirection = text.Any(ch => ch is >= '؀' and <= 'ۿ')
                ? System.Windows.FlowDirection.RightToLeft
                : System.Windows.FlowDirection.LeftToRight,
        };
        InkCanvas.SetLeft(block, pos.X);
        InkCanvas.SetTop (block, pos.Y);
        Canvas.Children.Add(block);
    }

    private void DemoStroke(IEnumerable<Point> pts, string color, double t)
    {
        var stroke = new Stroke(new StylusPointCollection(pts.Select(p => new StylusPoint(p.X, p.Y))))
        {
            DrawingAttributes = new DrawingAttributes
            {
                Color = Hex(color), Width = t, Height = t,
                FitToCurve = true, StylusTip = StylusTip.Ellipse,
            },
        };
        Canvas.Strokes.Add(stroke);
    }

    // Slightly wavy hand-drawn underline
    private void DemoWave(double x1, double x2, double y, string color, double t)
    {
        var pts = new List<Point>();
        for (double x = x1; x <= x2; x += 8)
            pts.Add(new(x, y + Math.Sin((x - x1) / 38.0) * 3.5));
        DemoStroke(pts, color, t);
    }

    // Hand-drawn loop around something (slightly overlapping ends, like a real pen)
    private void DemoLoop(Point c, double rx, double ry, string color, double t)
    {
        var pts = new List<Point>();
        for (double a = -0.3; a <= 2 * Math.PI + 0.35; a += 0.04)
        {
            double wobble = 1 + 0.03 * Math.Sin(a * 3);
            pts.Add(new(c.X + rx * wobble * Math.Cos(a), c.Y + ry * wobble * Math.Sin(a) + a * 3));
        }
        DemoStroke(pts, color, t);
    }

    // Curve on the black-board axes: origin (320, 620), π = 300 px, amplitude 200 px
    private void DemoCurve(Func<double, double> f, string color, double t)
    {
        var pts = new List<Point>();
        for (double px = 0; px <= 1200; px += 6)
            pts.Add(new(320 + px, 620 - 200 * f(px / 300.0 * Math.PI)));
        DemoStroke(pts, color, t);
    }
}
#endif
