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

using ScreenWriter.Models;

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
        ApplyClickThrough(!_drawingMode);
        if (!_drawingMode)
            Canvas.EditingMode = InkCanvasEditingMode.None;
        else
            SetTool(_currentTool);   // restore correct editing mode
    }

    public void SetTool(DrawingTool tool)
    {
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
        if (_currentTool is DrawingTool.Pen or DrawingTool.Eraser) return;
        _shapeStart   = e.GetPosition(Canvas);
        _previewShape = BuildShape(_shapeStart, _shapeStart);
        Canvas.Children.Add(_previewShape);
        Mouse.Capture(Canvas);
        e.Handled = true;
    }

    private void OnShapeMouseMove(object sender, MouseEventArgs e)
    {
        if (_previewShape is null) return;
        UpdateShape(_previewShape, _shapeStart, e.GetPosition(Canvas));
        e.Handled = true;
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

    public void ClearAll()
    {
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
        if (Canvas.Strokes.Count == 0 && Canvas.Children.Count == 0) return;

        bool wasDrawing = _drawingMode;
        if (wasDrawing) ApplyClickThrough(true);

        var result = MessageBox.Show(
            "هل تريد مسح جميع الرسومات؟",
            "مسح الكل",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (wasDrawing) ApplyClickThrough(false);
        if (result == MessageBoxResult.Yes) ClearAll();
    }
}
