// Board-only features: images, rendering, and converting a board to and from a file.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreenWriter.Models;
using ScreenWriter.Services;
using Brushes        = System.Windows.Media.Brushes;
using Color          = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using DragEventArgs  = System.Windows.DragEventArgs;
using DataFormats    = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using FontFamily     = System.Windows.Media.FontFamily;
using Image          = System.Windows.Controls.Image;
using Path           = System.Windows.Shapes.Path;
using Point          = System.Windows.Point;
using Rectangle      = System.Windows.Shapes.Rectangle;
using TextBox        = System.Windows.Controls.TextBox;

namespace ScreenWriter.Windows;

public partial class OverlayWindow
{
    private const double ImageFitRatio = 0.7;
    private const double MinImageSide  = 20;

    /// <summary>Asks the toolbar to change tool, so its buttons stay in sync.</summary>
    public event Action<DrawingTool>? ToolSwitchRequested;
    /// <summary>Files dropped on a board, with the drop position.</summary>
    public event Action<string[], Point>? FilesDropped;

    public bool IsBoard => _mode != BoardMode.Transparent;

    public string? FilePath => _boards[_mode].FilePath;

    public bool NeedsSave(BoardMode mode)
    {
        var board = _boards[mode];
        return mode != BoardMode.Transparent
            && board.Dirty
            && (CanvasHasContent(board.Canvas) || board.FilePath is not null);
    }

    /// <summary>Paste into the open text box (the global Ctrl+V hotkey swallows the keys).</summary>
    public void TextBoxPaste()
    {
        if (_activeInputWindow?.Content is TextBox tb) tb.Paste();
    }

    /// <summary>Finishes any text edit and removes selection outlines.</summary>
    public void FinishEditing() => PrepareForHistory();

    private void InitBoardDrop()
    {
        foreach (var canvas in new[] { WhiteCanvas, BlackCanvas })
        {
            canvas.AllowDrop = true;
            canvas.DragOver += OnBoardDragOver;
            canvas.Drop     += OnBoardDrop;
        }
    }

    private void OnBoardDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnBoardDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0) return;
        var position = e.GetPosition(Canvas);
        e.Handled = true;
        // Finish the drag first: dialogs must not open inside the drop callback.
        Dispatcher.BeginInvoke(() => FilesDropped?.Invoke(paths, position));
    }

    // ── Images ────────────────────────────────────────────────────────────────
    public void InsertImage(BoardImage data, Point? center = null)
    {
        if (!IsBoard) return;
        FinishEditing();

        double scale = Math.Min(1, Math.Min(
            Canvas.ActualWidth  * ImageFitRatio / data.Bitmap.PixelWidth,
            Canvas.ActualHeight * ImageFitRatio / data.Bitmap.PixelHeight));
        double w = Math.Max(MinImageSide, data.Bitmap.PixelWidth  * scale);
        double h = Math.Max(MinImageSide, data.Bitmap.PixelHeight * scale);

        var c = center ?? new Point(Canvas.ActualWidth / 2, Canvas.ActualHeight / 2);
        var image = CreateImage(data, c.X - w / 2, c.Y - h / 2, w, h);

        Canvas.Children.Add(image);
        var canvas = Canvas;
        _redoStack.Clear();
        _undoStack.Push(new(
            Undo: () => canvas.Children.Remove(image),
            Redo: () => canvas.Children.Add(image)));

        ToolSwitchRequested?.Invoke(DrawingTool.Select);
        SelectElement(image);
    }

    // Stays beneath drawings whatever order elements are added in (undo re-adds them last),
    // and lets the pen write across it.
    private static Image CreateImage(BoardImage data, double x, double y, double w, double h)
    {
        var image = new Image
        {
            Source  = data.Bitmap,
            Width   = w,
            Height  = h,
            Stretch = Stretch.Fill,
            Tag     = data,
            IsHitTestVisible = false,
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        System.Windows.Controls.Panel.SetZIndex(image, -1);
        InkCanvas.SetLeft(image, x);
        InkCanvas.SetTop (image, y);
        return image;
    }

    // Images lie under everything, so they are hit only after shapes, text and ink.
    private Image? HitTestImage(Point pos)
    {
        foreach (var image in Canvas.Children.OfType<Image>().Reverse())
            if (GetElementBounds(image).Contains(pos)) return image;
        return null;
    }

    // Corner handles scale the picture evenly and keep the opposite corner in place.
    private void ResizeImage(Image image, double dx, double dy)
    {
        double w = _resizeOrigW, h = _resizeOrigH;

        double scale = _activeHandle switch
        {
            ResizeHandle.TopLeft     => Math.Max((w - dx) / w, (h - dy) / h),
            ResizeHandle.TopRight    => Math.Max((w + dx) / w, (h - dy) / h),
            ResizeHandle.BottomLeft  => Math.Max((w - dx) / w, (h + dy) / h),
            ResizeHandle.BottomRight => Math.Max((w + dx) / w, (h + dy) / h),
            _                        => 1,
        };
        scale = Math.Max(scale, MinImageSide / Math.Min(w, h));

        double newW = w * scale, newH = h * scale;
        double left = _activeHandle is ResizeHandle.TopLeft or ResizeHandle.BottomLeft
            ? _resizeOrigLeft + w - newW : _resizeOrigLeft;
        double top  = _activeHandle is ResizeHandle.TopLeft or ResizeHandle.TopRight
            ? _resizeOrigTop + h - newH : _resizeOrigTop;

        ApplyBoundsToElement(image, left, top, newW, newH);
    }

    // ── Render ────────────────────────────────────────────────────────────────
    /// <summary>The current board as a bitmap at the screen's real resolution.</summary>
    public BitmapSource RenderBoard()
    {
        FinishEditing();

        var dpi = VisualTreeHelper.GetDpi(Canvas);
        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(Canvas.ActualWidth  * dpi.DpiScaleX),
            (int)Math.Ceiling(Canvas.ActualHeight * dpi.DpiScaleY),
            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(Canvas);
        bitmap.Freeze();
        return bitmap;
    }

    // ── Save / open ───────────────────────────────────────────────────────────
    /// <summary>Writes the current board to a .swboard file.</summary>
    public void SaveBoard(string path)
    {
        FinishEditing();

        var page   = new BoardPage();
        var images = new Dictionary<string, byte[]>();
        var names  = new Dictionary<BoardImage, string>();

        foreach (UIElement child in Canvas.Children)
        {
            if (ExportElement(child, images, names) is { } element)
                page.Elements.Add(element);
        }

        var document = new BoardDocument
        {
            FormatVersion = BoardFile.CurrentVersion,
            Board  = _mode.ToString(),
            Width  = Canvas.ActualWidth,
            Height = Canvas.ActualHeight,
            Pages  = [page],
        };
        BoardFile.Save(path, document, Canvas.Strokes, images);

        _boards[_mode].FilePath = path;
        _boards[_mode].Dirty    = false;
    }

    private BoardElement? ExportElement(UIElement child, Dictionary<string, byte[]> images,
        Dictionary<BoardImage, string> names)
    {
        switch (child)
        {
            case Rectangle r:
                return new BoardElement { Type = "rect", X = InkCanvas.GetLeft(r), Y = InkCanvas.GetTop(r),
                    W = r.Width, H = r.Height, Color = ColorOf(r.Stroke), Thickness = r.StrokeThickness };

            case Ellipse e:
                return new BoardElement { Type = "ellipse", X = InkCanvas.GetLeft(e), Y = InkCanvas.GetTop(e),
                    W = e.Width, H = e.Height, Color = ColorOf(e.Stroke), Thickness = e.StrokeThickness };

            case Line l:
                return new BoardElement { Type = "line", X1 = l.X1, Y1 = l.Y1, X2 = l.X2, Y2 = l.Y2,
                    Color = ColorOf(l.Stroke), Thickness = l.StrokeThickness };

            case Path p when _arrowPoints.TryGetValue(p, out var pts):
                return new BoardElement { Type = "arrow", X1 = pts.P1.X, Y1 = pts.P1.Y, X2 = pts.P2.X, Y2 = pts.P2.Y,
                    Color = ColorOf(p.Stroke), Thickness = p.StrokeThickness };

            case TextBlock t:
                return new BoardElement { Type = "text", X = InkCanvas.GetLeft(t), Y = InkCanvas.GetTop(t),
                    Color = ColorOf(t.Foreground), Text = t.Text, FontSize = t.FontSize,
                    Bold = t.FontWeight == FontWeights.Bold,
                    Rtl  = t.FlowDirection == System.Windows.FlowDirection.RightToLeft };

            case Image { Tag: BoardImage data } image:
                if (!names.TryGetValue(data, out var name))
                {
                    name = BoardFile.NewImageName(data.Extension);
                    names[data]  = name;
                    images[name] = data.Bytes;
                }
                return new BoardElement { Type = "image", X = InkCanvas.GetLeft(image), Y = InkCanvas.GetTop(image),
                    W = image.Width, H = image.Height, Image = name };

            default:
                return null;
        }
    }

    private static string ColorOf(System.Windows.Media.Brush? brush)
        => brush is SolidColorBrush solid ? solid.Color.ToString() : "#FF000000";

    /// <summary>Replaces a board's content with a validated file and shows that board.</summary>
    public void ApplyBoard(LoadedBoard loaded, string path)
    {
        FinishEditing();
        SetBoardMode(loaded.Mode);
        UpdateLayout();

        // A file saved on a bigger screen is shrunk to fit this one.
        double scale = 1;
        if (Canvas.ActualWidth > 0 && Canvas.ActualHeight > 0)
            scale = Math.Min(1, Math.Min(Canvas.ActualWidth / loaded.Document.Width,
                                         Canvas.ActualHeight / loaded.Document.Height));

        ResetBoard(path);

        foreach (var element in loaded.Page.Elements)
            Canvas.Children.Add(BuildElement(element, scale, loaded.Images));

        if (scale < 1)
            loaded.Strokes.Transform(new Matrix(scale, 0, 0, scale, 0, 0), true);
        Canvas.Strokes.Add(loaded.Strokes);
        _boards[_mode].Dirty = false;
    }

    /// <summary>Empties the current board and detaches it from its file.</summary>
    public void NewBoard()
    {
        FinishEditing();
        ResetBoard(null);
    }

    private void ResetBoard(string? path)
    {
        foreach (var arrow in Canvas.Children.OfType<Path>().ToList())
            _arrowPoints.Remove(arrow);
        Canvas.Children.Clear();
        Canvas.Strokes.Clear();

        var board = _boards[_mode];
        board.Undo.Clear();
        board.Redo.Clear();
        board.FilePath = path;
        board.Dirty    = false;
    }

    private UIElement BuildElement(BoardElement e, double s, Dictionary<string, BoardImage> images)
    {
        if (e.Type == "image")
            return CreateImage(images[e.Image!], e.X * s, e.Y * s, e.W * s, e.H * s);

        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(e.Color!));
        brush.Freeze();
        double t = e.Thickness * s;

        switch (e.Type)
        {
            case "rect":
                return Placed(new Rectangle { Width = e.W * s, Height = e.H * s,
                    Stroke = brush, StrokeThickness = t, Fill = Brushes.Transparent }, e.X * s, e.Y * s);

            case "ellipse":
                return Placed(new Ellipse { Width = e.W * s, Height = e.H * s,
                    Stroke = brush, StrokeThickness = t, Fill = Brushes.Transparent }, e.X * s, e.Y * s);

            case "line":
                return new Line { X1 = e.X1 * s, Y1 = e.Y1 * s, X2 = e.X2 * s, Y2 = e.Y2 * s,
                    Stroke = brush, StrokeThickness = t,
                    StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };

            case "arrow":
                return BuildArrow(new Point(e.X1 * s, e.Y1 * s), new Point(e.X2 * s, e.Y2 * s), brush, t);

            default:
                return Placed(new TextBlock
                {
                    Text          = e.Text,
                    Foreground    = brush,
                    FontFamily    = new FontFamily("Arial"),
                    FontSize      = Math.Max(1, e.FontSize * s),
                    FontWeight    = e.Bold ? FontWeights.Bold : FontWeights.Normal,
                    FlowDirection = e.Rtl ? System.Windows.FlowDirection.RightToLeft
                                          : System.Windows.FlowDirection.LeftToRight,
                    TextWrapping  = TextWrapping.Wrap,
                    MaxWidth      = 600 * s,
                }, e.X * s, e.Y * s);
        }
    }

    private static UIElement Placed(UIElement element, double x, double y)
    {
        InkCanvas.SetLeft(element, x);
        InkCanvas.SetTop (element, y);
        return element;
    }
}
