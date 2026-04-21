using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Color          = System.Windows.Media.Color;
using MessageBox     = System.Windows.MessageBox;
using Point          = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Rectangle      = System.Windows.Shapes.Rectangle;
using Brushes        = System.Windows.Media.Brushes;
using TextBox        = System.Windows.Controls.TextBox;

using ScreenWriter.Models;
using ScreenWriter.Services;

namespace ScreenWriter.Windows;

public partial class OverlayWindow : Window
{
    // ── Win32 ────────────────────────────────────────────────────────────────
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLong(nint hwnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLong(nint hwnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private const int  GWL_EXSTYLE       = -20;
    private const nint WS_EX_TRANSPARENT = 0x00000020;
    private const nint WS_EX_LAYERED     = 0x00080000;
    private const nint WS_EX_NOACTIVATE  = 0x08000000;
    private static readonly nint HWND_TOPMOST = new(-1);
    private const uint SWP_NOMOVE        = 0x0002;
    private const uint SWP_NOSIZE        = 0x0001;

    private const int WM_MOUSEACTIVATE   = 0x0021;
    private const int MA_NOACTIVATE      = 3;

    // ── History ───────────────────────────────────────────────────────────────
    private record HistoryEntry(Action Undo, Action Redo);
    private readonly Stack<HistoryEntry> _undoStack = new();
    private readonly Stack<HistoryEntry> _redoStack = new();

    // ── Drawing state ─────────────────────────────────────────────────────────
    private DrawingTool _currentTool    = DrawingTool.Pen;
    private Color       _currentColor   = Colors.Red;
    private double      _currentPenSize = 4;

    private Point  _shapeStart;
    private Shape? _previewShape;

    // ── Text tool state ───────────────────────────────────────────────────────
    private Window?           _activeInputWindow;
    private Point             _activeTextPos;
    private double            _activeTextFontSize;
    private SolidColorBrush?  _activeTextBrush;

    private bool _drawingMode;
    public bool IsDrawingMode => _drawingMode;

    // ─────────────────────────────────────────────────────────────────────────
    public OverlayWindow()
    {
        InitializeComponent();
        Loaded            += OnLoaded;
        SourceInitialized += OnSourceInitialized;

        Canvas.StrokeCollected += (_, e) =>
        {
            var stroke = e.Stroke;
            _redoStack.Clear();
            _undoStack.Push(new(
                Undo: () => Canvas.Strokes.Remove(stroke),
                Redo: () => Canvas.Strokes.Add(stroke)));
        };

        Canvas.MouseDown += OnShapeMouseDown;
        Canvas.MouseMove += OnShapeMouseMove;
        Canvas.MouseUp   += OnShapeMouseUp;

        SetDefaultAttributes();
    }

    // ── Win32 hooks ───────────────────────────────────────────────────────────
    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(WndProc);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WM_MOUSEACTIVATE) { handled = true; return MA_NOACTIVATE; }
        return nint.Zero;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Left   = SystemParameters.VirtualScreenLeft;
        Top    = SystemParameters.VirtualScreenTop;
        Width  = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        var hwnd  = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_NOACTIVATE);
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        ApplyClickThrough(true);
    }

    // ── Mode / tool ───────────────────────────────────────────────────────────
    public void ToggleDrawingMode()
    {
        _drawingMode = !_drawingMode;
        if (!_drawingMode)
        {
            CommitTextBox();
            ApplyClickThrough(true);
            Canvas.EditingMode = InkCanvasEditingMode.None;
        }
        else
        {
            ApplyClickThrough(false);
            SetTool(_currentTool);   // restore correct editing mode
        }
    }

    public void SetTool(DrawingTool tool)
    {
        CommitTextBox(); // commit any pending text before switching tools
        _currentTool = tool;
        Canvas.EditingMode = tool switch
        {
            DrawingTool.Pen    => InkCanvasEditingMode.Ink,
            DrawingTool.Eraser => InkCanvasEditingMode.EraseByPoint,
            _                  => InkCanvasEditingMode.None,
        };
    }

    private void ApplyClickThrough(bool enable)
    {
        var hwnd  = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        var ns    = enable
            ? (style | WS_EX_TRANSPARENT | WS_EX_LAYERED) | WS_EX_NOACTIVATE
            : (style & ~WS_EX_TRANSPARENT)                | WS_EX_NOACTIVATE;
        SetWindowLong(hwnd, GWL_EXSTYLE, ns);
    }

    // ── Drawing attributes ────────────────────────────────────────────────────
    private void SetDefaultAttributes()
    {
        Canvas.DefaultDrawingAttributes = new DrawingAttributes
        {
            Color      = _currentColor,
            Width      = _currentPenSize,
            Height     = _currentPenSize,
            FitToCurve = true,
            StylusTip  = StylusTip.Ellipse,
        };
    }

    public void SetColor(Color color)
    {
        _currentColor = color;
        var attr = Canvas.DefaultDrawingAttributes.Clone();
        attr.Color = color;
        Canvas.DefaultDrawingAttributes = attr;
        if (_currentTool != DrawingTool.Eraser)
            SetTool(_currentTool);
    }

    public void SetPenSize(double size)
    {
        _currentPenSize = size;
        var attr = Canvas.DefaultDrawingAttributes.Clone();
        attr.Width = attr.Height = size;
        Canvas.DefaultDrawingAttributes = attr;
    }

    public void SetEraser(bool eraser)
        => SetTool(eraser ? DrawingTool.Eraser : DrawingTool.Pen);

    // ── Shape mouse events ────────────────────────────────────────────────────
    private void OnShapeMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_currentTool is DrawingTool.Pen or DrawingTool.Eraser)
        {
            // Eraser tool: also hit-test UIElement children (shapes & text)
            if (_currentTool == DrawingTool.Eraser)
                EraseChildAt(e.GetPosition(Canvas));
            return;
        }

        if (_currentTool == DrawingTool.Text)
        {
            PlaceTextBox(e.GetPosition(Canvas));
            e.Handled = true;
            return;
        }

        _shapeStart   = e.GetPosition(Canvas);
        _previewShape = BuildShape(_shapeStart, _shapeStart);
        Canvas.Children.Add(_previewShape);
        Mouse.Capture(Canvas);
        e.Handled = true;
    }

    private void OnShapeMouseMove(object sender, MouseEventArgs e)
    {
        // Erase while dragging
        if (_currentTool == DrawingTool.Eraser && e.LeftButton == MouseButtonState.Pressed)
            EraseChildAt(e.GetPosition(Canvas));

        if (_previewShape is null) return;
        UpdateShape(_previewShape, _shapeStart, e.GetPosition(Canvas));
        e.Handled = true;
    }

    private void EraseChildAt(Point pos)
    {
        const double HitRadius = 20;
        UIElement? hit = null;
        foreach (UIElement child in Canvas.Children)
        {
            var left = InkCanvas.GetLeft(child);
            var top  = InkCanvas.GetTop(child);
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top))  top  = 0;

            child.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            var w = child.DesiredSize.Width;
            var h = child.DesiredSize.Height;

            var rect = new Rect(left - HitRadius, top - HitRadius,
                                w + HitRadius * 2, h + HitRadius * 2);
            if (rect.Contains(pos)) { hit = child; break; }
        }
        if (hit is null) return;

        Canvas.Children.Remove(hit);
        var removed = hit;
        _redoStack.Clear();
        _undoStack.Push(new(
            Undo: () => Canvas.Children.Add(removed),
            Redo: () => Canvas.Children.Remove(removed)));
    }

    private void OnShapeMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_previewShape is null) return;
        Mouse.Capture(null);

        var (_, _, w, h) = NormalizeRect(_shapeStart, e.GetPosition(Canvas));
        if (w < 2 && h < 2 && _currentTool != DrawingTool.Line)
        {
            Canvas.Children.Remove(_previewShape);
            _previewShape = null;
            return;
        }

        var final = _previewShape;
        _previewShape = null;
        _redoStack.Clear();
        _undoStack.Push(new(
            Undo: () => Canvas.Children.Remove(final),
            Redo: () => Canvas.Children.Add(final)));
        e.Handled = true;
    }

    private Shape BuildShape(Point a, Point b)
    {
        var brush = new SolidColorBrush(_currentColor);
        var t     = _currentPenSize;
        var (x, y, w, h) = NormalizeRect(a, b);

        return _currentTool switch
        {
            DrawingTool.Line => new Line
            {
                X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y,
                Stroke = brush, StrokeThickness = t,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap   = PenLineCap.Round,
            },
            DrawingTool.Rectangle => Positioned(new Rectangle
            {
                Width = Math.Max(w, 1), Height = Math.Max(h, 1),
                Stroke = brush, StrokeThickness = t, Fill = Brushes.Transparent,
            }, x, y),
            DrawingTool.Ellipse => Positioned(new Ellipse
            {
                Width = Math.Max(w, 1), Height = Math.Max(h, 1),
                Stroke = brush, StrokeThickness = t, Fill = Brushes.Transparent,
            }, x, y),
            _ => throw new InvalidOperationException(),
        };
    }

    private static void UpdateShape(Shape shape, Point a, Point b)
    {
        var (x, y, w, h) = NormalizeRect(a, b);
        switch (shape)
        {
            case Line line:
                line.X2 = b.X; line.Y2 = b.Y;
                break;
            case Rectangle rect:
                rect.Width = Math.Max(w, 1); rect.Height = Math.Max(h, 1);
                Positioned(rect, x, y);
                break;
            case Ellipse el:
                el.Width = Math.Max(w, 1); el.Height = Math.Max(h, 1);
                Positioned(el, x, y);
                break;
        }
    }

    private static T Positioned<T>(T shape, double x, double y) where T : Shape
    {
        InkCanvas.SetLeft(shape, x);
        InkCanvas.SetTop(shape, y);
        return shape;
    }

    private static (double x, double y, double w, double h) NormalizeRect(Point p1, Point p2)
        => (Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
            Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));

    // ── Undo / Redo ───────────────────────────────────────────────────────────
    public void Undo()
    {
        if (_undoStack.TryPop(out var e)) { e.Undo(); _redoStack.Push(e); }
    }

    public void Redo()
    {
        if (_redoStack.TryPop(out var e)) { e.Redo(); _undoStack.Push(e); }
    }

    // ── Text tool ─────────────────────────────────────────────────────────────
    // Uses a separate floating input window so the overlay never loses
    // WS_EX_NOACTIVATE and never floats above the toolbar.
    private void PlaceTextBox(Point canvasPos)
    {
        CommitTextBox(); // save any existing text before opening a new one

        _activeTextPos      = canvasPos;
        _activeTextFontSize = Math.Max(16, _currentPenSize * 4);
        _activeTextBrush    = new SolidColorBrush(_currentColor);

        var tb = new TextBox
        {
            Background      = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
            Foreground      = _activeTextBrush,
            CaretBrush      = _activeTextBrush,
            BorderBrush     = new SolidColorBrush(Color.FromArgb(120,
                _currentColor.R, _currentColor.G, _currentColor.B)),
            BorderThickness = new Thickness(1),
            FontFamily      = new System.Windows.Media.FontFamily("Arial"),
            FontSize        = _activeTextFontSize,
            MinWidth        = 120,
            AcceptsReturn   = true,
            TextWrapping    = TextWrapping.Wrap,
            MaxWidth        = 600,
        };

        var screenPt = Canvas.PointToScreen(canvasPos);
        var win = new Window
        {
            WindowStyle        = WindowStyle.None,
            AllowsTransparency = true,
            Background         = Brushes.Transparent,
            Topmost            = true,
            ShowInTaskbar      = false,
            SizeToContent      = SizeToContent.WidthAndHeight,
            Left               = screenPt.X,
            Top                = screenPt.Y,
            Title              = "",
            Content            = tb,
        };

        _activeInputWindow = win;

        tb.LostFocus += (_, _) => Dispatcher.BeginInvoke(CommitTextBox);
        tb.KeyDown   += (_, e) =>
        {
            if (e.Key == Key.Escape) { CancelTextBox(); e.Handled = true; }
        };

        win.Show();
        tb.Focus();
    }

    private void CommitTextBox()
    {
        if (_activeInputWindow is null) return;
        var win = _activeInputWindow;
        _activeInputWindow = null;

        var tb   = (TextBox)win.Content;
        var text = tb.Text.Trim();
        var pos  = _activeTextPos;
        win.Close();

        if (string.IsNullOrEmpty(text)) return;

        var block = new TextBlock
        {
            Text         = text,
            Foreground   = _activeTextBrush,
            FontFamily   = new System.Windows.Media.FontFamily("Arial"),
            FontSize     = _activeTextFontSize,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth     = 600,
        };
        InkCanvas.SetLeft(block, pos.X);
        InkCanvas.SetTop (block, pos.Y);
        Canvas.Children.Add(block);

        var final = block;
        _redoStack.Clear();
        _undoStack.Push(new(
            Undo: () => Canvas.Children.Remove(final),
            Redo: () => Canvas.Children.Add(final)));
    }

    private void CancelTextBox()
    {
        if (_activeInputWindow is null) return;
        var win = _activeInputWindow;
        _activeInputWindow = null;
        win.Close();
    }

    public void ClearAll()
    {
        CancelTextBox();
        if (Canvas.Strokes.Count == 0 && Canvas.Children.Count == 0) return;

        var strokes = new StrokeCollection(Canvas.Strokes);
        var shapes  = Canvas.Children.Cast<UIElement>().ToList();

        _redoStack.Clear();
        _undoStack.Push(new(
            Undo: () =>
            {
                Canvas.Strokes.Clear();
                foreach (var s in strokes) Canvas.Strokes.Add(s);
                Canvas.Children.Clear();
                foreach (var sh in shapes) Canvas.Children.Add(sh);
            },
            Redo: () => { Canvas.Strokes.Clear(); Canvas.Children.Clear(); }));

        Canvas.Strokes.Clear();
        Canvas.Children.Clear();
    }

    public void ConfirmedClearAll()
    {
        CommitTextBox();
        if (Canvas.Strokes.Count == 0 && Canvas.Children.Count == 0) return;

        bool wasDrawing = _drawingMode;
        if (wasDrawing) ApplyClickThrough(true);

        var svc    = LocalizationService.Instance;
        var result = MessageBox.Show(
            svc.Get("Str_ClearConfirmMsg"),
            svc.Get("Str_ClearConfirmTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (wasDrawing) ApplyClickThrough(false);
        if (result == MessageBoxResult.Yes) ClearAll();
    }
}
