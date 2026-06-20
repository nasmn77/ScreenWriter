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
using Path           = System.Windows.Shapes.Path;
using Cursor         = System.Windows.Input.Cursor;
using Cursors        = System.Windows.Input.Cursors;

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

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<double?>? SelectionThicknessChanged;

    // ── Arrow endpoints store ─────────────────────────────────────────────────
    private readonly Dictionary<Path, (Point P1, Point P2)> _arrowPoints = [];

    // ── Select tool state ─────────────────────────────────────────────────────
    private enum ResizeHandle { None, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }

    private UIElement?    _selectedElement;
    private ResizeHandle  _activeHandle    = ResizeHandle.None;
    private bool          _isDragging;
    private Point         _dragStart;
    private double        _dragOrigLeft, _dragOrigTop;

    // For resize: original element bounds at drag start
    private double _resizeOrigLeft, _resizeOrigTop, _resizeOrigW, _resizeOrigH;
    // For Line/Arrow resize: original endpoints
    private Point _resizeOrigP1, _resizeOrigP2;

    // Selection overlay elements (owned by Canvas, removed on deselect)
    private Rectangle?   _selectionRect;
    private Rectangle[]? _handles;         // 8 handles, indexed by ResizeHandle-1

    // Selected Stroke (freehand pen)
    private Stroke? _selectedStroke;
    private Point   _strokeDragStart;
    private StylusPointCollection? _strokeOrigPoints;
    private Rect    _strokeOrigBounds;

    // Hover highlight (shown on mouseover in Select/Eraser mode)
    private Rectangle?  _hoverRect;
    private UIElement?  _hoveredElement;
    private Stroke?     _hoveredStroke;

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
            Deselect();
            DeselectStroke();
            HideHoverRect();
            ApplyClickThrough(true);
            Canvas.EditingMode = InkCanvasEditingMode.None;
        }
        else
        {
            ApplyClickThrough(false);
            SetTool(_currentTool);
        }
    }

    public void SetTool(DrawingTool tool)
    {
        CommitTextBox();
        Deselect();
        DeselectStroke();
        HideHoverRect();
        _currentTool = tool;
        Canvas.EditingMode = tool switch
        {
            DrawingTool.Pen    => InkCanvasEditingMode.Ink,
            DrawingTool.Eraser => InkCanvasEditingMode.None,
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

        // Apply to selected element/stroke if any
        if (_selectedElement is not null)
        {
            ApplyColorToElement(_selectedElement, color);
            return;
        }
        if (_selectedStroke is not null)
        {
            var oldColor = _selectedStroke.DrawingAttributes.Color;
            var stroke   = _selectedStroke;
            _selectedStroke.DrawingAttributes = new DrawingAttributes
            {
                Color      = color,
                Width      = _selectedStroke.DrawingAttributes.Width,
                Height     = _selectedStroke.DrawingAttributes.Height,
                FitToCurve = _selectedStroke.DrawingAttributes.FitToCurve,
                StylusTip  = _selectedStroke.DrawingAttributes.StylusTip,
            };
            _redoStack.Clear();
            _undoStack.Push(new(
                Undo: () => stroke.DrawingAttributes = new DrawingAttributes { Color = oldColor, Width = stroke.DrawingAttributes.Width, Height = stroke.DrawingAttributes.Height, FitToCurve = stroke.DrawingAttributes.FitToCurve, StylusTip = stroke.DrawingAttributes.StylusTip },
                Redo: () => stroke.DrawingAttributes = new DrawingAttributes { Color = color,    Width = stroke.DrawingAttributes.Width, Height = stroke.DrawingAttributes.Height, FitToCurve = stroke.DrawingAttributes.FitToCurve, StylusTip = stroke.DrawingAttributes.StylusTip }));
            return;
        }

        if (_currentTool != DrawingTool.Eraser)
            SetTool(_currentTool);
    }

    private void ApplyColorToElement(UIElement el, Color color)
    {
        var brush = new SolidColorBrush(color);
        switch (el)
        {
            case Shape s:
                var oldStroke = s.Stroke;
                var oldFill   = s.Fill;
                s.Stroke = brush;
                if (s is Path && s.Fill != Brushes.Transparent) s.Fill = brush;
                _redoStack.Clear();
                _undoStack.Push(new(
                    Undo: () => { s.Stroke = oldStroke; s.Fill = oldFill; },
                    Redo: () => { s.Stroke = brush;     if (s is Path && oldFill != Brushes.Transparent) s.Fill = brush; }));
                break;
            case TextBlock tb:
                var oldFg = tb.Foreground;
                tb.Foreground = brush;
                _redoStack.Clear();
                _undoStack.Push(new(
                    Undo: () => tb.Foreground = oldFg,
                    Redo: () => tb.Foreground = brush));
                break;
        }
    }

    public void SetPenSize(double size)
    {
        _currentPenSize = size;
        var attr = Canvas.DefaultDrawingAttributes.Clone();
        attr.Width = attr.Height = size;
        Canvas.DefaultDrawingAttributes = attr;

        // Apply to selected element/stroke if any (not TextBlock)
        if (_selectedElement is Shape s && _selectedElement is not TextBlock)
        {
            var oldThickness = s.StrokeThickness;
            s.StrokeThickness = size;
            // For Arrow: recompute geometry with new thickness
            if (s is Path arrow && _arrowPoints.TryGetValue(arrow, out var pts))
                arrow.Data = ComputeArrowGeometry(pts.P1, pts.P2, size);
            _redoStack.Clear();
            _undoStack.Push(new(
                Undo: () => { s.StrokeThickness = oldThickness; if (s is Path a && _arrowPoints.TryGetValue(a, out var p)) a.Data = ComputeArrowGeometry(p.P1, p.P2, oldThickness); },
                Redo: () => { s.StrokeThickness = size;         if (s is Path a && _arrowPoints.TryGetValue(a, out var p)) a.Data = ComputeArrowGeometry(p.P1, p.P2, size); }));
        }
        else if (_selectedStroke is not null)
        {
            var stroke   = _selectedStroke;
            var oldSize  = stroke.DrawingAttributes.Width;
            var newAttrs = stroke.DrawingAttributes.Clone();
            newAttrs.Width  = size;
            newAttrs.Height = size;
            stroke.DrawingAttributes = newAttrs;
            _redoStack.Clear();
            _undoStack.Push(new(
                Undo: () => { var a = stroke.DrawingAttributes.Clone(); a.Width = oldSize; a.Height = oldSize; stroke.DrawingAttributes = a; },
                Redo: () => { var a = stroke.DrawingAttributes.Clone(); a.Width = size;    a.Height = size;    stroke.DrawingAttributes = a; }));
        }
    }

    public void SetEraser(bool eraser)
        => SetTool(eraser ? DrawingTool.Eraser : DrawingTool.Pen);

    // ── Shape mouse events ────────────────────────────────────────────────────
    private void OnShapeMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_currentTool == DrawingTool.Pen) return;

        if (_currentTool == DrawingTool.Eraser)
        {
            EraseAt(e.GetPosition(Canvas));
            return;
        }

        if (_currentTool == DrawingTool.Text)
        {
            PlaceTextBox(e.GetPosition(Canvas));
            e.Handled = true;
            return;
        }

        if (_currentTool == DrawingTool.Select)
        {
            if (e.ClickCount == 2 && _selectedElement is TextBlock tbEdit)
            {
                EditTextBlock(tbEdit);
                e.Handled = true;
                return;
            }
            SelectMouseDown(e);
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
        if (_currentTool == DrawingTool.Eraser && e.LeftButton == MouseButtonState.Pressed)
            EraseAt(e.GetPosition(Canvas));

        // Hover highlight for Select and Eraser tools
        if (_currentTool is DrawingTool.Select or DrawingTool.Eraser)
            UpdateHoverRect(e.GetPosition(Canvas));

        if (_currentTool == DrawingTool.Select)
        {
            SelectMouseMove(e);
            return;
        }

        if (_previewShape is null) return;
        UpdateShape(_previewShape, _shapeStart, e.GetPosition(Canvas));
        e.Handled = true;
    }

    private void OnShapeMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentTool == DrawingTool.Select)
        {
            SelectMouseUp(e);
            return;
        }

        if (_previewShape is null) return;
        Mouse.Capture(null);

        var (_, _, w, h) = NormalizeRect(_shapeStart, e.GetPosition(Canvas));
        if (w < 2 && h < 2 && _currentTool is not DrawingTool.Line and not DrawingTool.Arrow)
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

    // ── Hover rect ────────────────────────────────────────────────────────────
    private void UpdateHoverRect(Point pos)
    {
        if (_selectedElement is not null || _selectedStroke is not null) { HideHoverRect(); return; }

        // Hit-test UIElement children first
        var hitEl = HitTestChild(pos);
        // Hit-test strokes
        Stroke? hitStroke = hitEl is null ? HitTestStroke(pos) : null;

        if (hitEl == _hoveredElement && hitStroke == _hoveredStroke) return;

        HideHoverRect();

        Rect bounds;
        if (hitEl is not null)
        {
            _hoveredElement = hitEl;
            bounds = GetElementBounds(hitEl);
        }
        else if (hitStroke is not null)
        {
            _hoveredStroke = hitStroke;
            bounds = hitStroke.GetBounds();
        }
        else return;

        const double pad = 3;
        _hoverRect = new Rectangle
        {
            Width            = bounds.Width  + pad * 2,
            Height           = bounds.Height + pad * 2,
            Stroke           = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            StrokeThickness  = 1.2,
            StrokeDashArray  = new DoubleCollection([4, 3]),
            Fill             = Brushes.Transparent,
            IsHitTestVisible = false,
        };
        InkCanvas.SetLeft(_hoverRect, bounds.Left - pad);
        InkCanvas.SetTop (_hoverRect, bounds.Top  - pad);
        Canvas.Children.Add(_hoverRect);
    }

    private void HideHoverRect()
    {
        if (_hoverRect is not null)
        {
            Canvas.Children.Remove(_hoverRect);
            _hoverRect = null;
        }
        _hoveredElement = null;
        _hoveredStroke  = null;
    }

    // ── Select tool ───────────────────────────────────────────────────────────
    private void SelectMouseDown(MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(Canvas);

        // Check if clicking a resize handle
        if (_handles is not null)
        {
            for (int i = 0; i < _handles.Length; i++)
            {
                var hRect   = _handles[i];
                var hLeft   = InkCanvas.GetLeft(hRect);
                var hTop    = InkCanvas.GetTop(hRect);
                var hBounds = new Rect(hLeft, hTop, hRect.Width, hRect.Height);
                if (hBounds.Contains(pos) && hRect.Visibility == Visibility.Visible)
                {
                    _activeHandle = (ResizeHandle)(i + 1);
                    _dragStart    = pos;
                    if (_selectedStroke is not null)
                    {
                        // Stroke resize — capture fresh origin at drag start
                        _strokeDragStart  = pos;
                        _strokeOrigPoints = new StylusPointCollection(_selectedStroke.StylusPoints);
                        _strokeOrigBounds = _selectedStroke.GetBounds();
                    }
                    else
                    {
                        CaptureResizeOrigin();
                    }
                    Mouse.Capture(Canvas);
                    return;
                }
            }
        }

        // Hit-test UIElement children
        var hit = HitTestChild(pos);
        if (hit is not null)
        {
            HideHoverRect();
            DeselectStroke();
            SelectElement(hit);
            _isDragging   = true;
            _activeHandle = ResizeHandle.None;
            _dragStart    = pos;
            var b = GetElementBounds(hit);
            _dragOrigLeft = b.Left;
            _dragOrigTop  = b.Top;
            Mouse.Capture(Canvas);
            return;
        }

        // Hit-test strokes
        var hitStroke = HitTestStroke(pos);
        if (hitStroke is not null)
        {
            HideHoverRect();
            Deselect();
            SelectStroke(hitStroke, pos);
            Mouse.Capture(Canvas);
            return;
        }

        Deselect();
        DeselectStroke();
    }

    private static Cursor HandleToCursor(ResizeHandle h) => h switch
    {
        ResizeHandle.TopLeft     => Cursors.SizeNWSE,
        ResizeHandle.Top         => Cursors.SizeNS,
        ResizeHandle.TopRight    => Cursors.SizeNESW,
        ResizeHandle.Right       => Cursors.SizeWE,
        ResizeHandle.BottomRight => Cursors.SizeNWSE,
        ResizeHandle.Bottom      => Cursors.SizeNS,
        ResizeHandle.BottomLeft  => Cursors.SizeNESW,
        ResizeHandle.Left        => Cursors.SizeWE,
        _                        => Cursors.Arrow,
    };

    private void SelectMouseMove(MouseEventArgs e)
    {
        var pos = e.GetPosition(Canvas);

        // Update cursor based on handle under mouse (when not actively dragging/resizing)
        if (e.LeftButton == MouseButtonState.Released)
        {
            var hoveredHandle = ResizeHandle.None;
            if (_handles is not null)
            {
                for (int i = 0; i < _handles.Length; i++)
                {
                    var h = _handles[i];
                    if (h.Visibility != Visibility.Visible) continue;
                    var hL = InkCanvas.GetLeft(h);
                    var hT = InkCanvas.GetTop(h);
                    var hs = h.Width;
                    if (pos.X >= hL && pos.X <= hL + hs && pos.Y >= hT && pos.Y <= hT + hs)
                    {
                        hoveredHandle = (ResizeHandle)(i + 1);
                        break;
                    }
                }
            }
            Canvas.Cursor = hoveredHandle != ResizeHandle.None
                ? HandleToCursor(hoveredHandle)
                : (_selectedElement is not null || _selectedStroke is not null ? Cursors.SizeAll : Cursors.Arrow);
        }

        if (_selectedStroke is not null && e.LeftButton == MouseButtonState.Pressed)
        {
            if (_activeHandle != ResizeHandle.None)
            {
                // Stroke resize
                var dx = pos.X - _strokeDragStart.X;
                var dy = pos.Y - _strokeDragStart.Y;
                var ob = _strokeOrigBounds;
                double newL = ob.Left, newT = ob.Top, newW = ob.Width, newH = ob.Height;
                switch (_activeHandle)
                {
                    case ResizeHandle.TopLeft:
                        newL = ob.Left + dx; newT = ob.Top + dy; newW = ob.Width - dx; newH = ob.Height - dy; break;
                    case ResizeHandle.Top:
                        newT = ob.Top + dy; newH = ob.Height - dy; break;
                    case ResizeHandle.TopRight:
                        newT = ob.Top + dy; newW = ob.Width + dx; newH = ob.Height - dy; break;
                    case ResizeHandle.Right:
                        newW = ob.Width + dx; break;
                    case ResizeHandle.BottomRight:
                        newW = ob.Width + dx; newH = ob.Height + dy; break;
                    case ResizeHandle.Bottom:
                        newH = ob.Height + dy; break;
                    case ResizeHandle.BottomLeft:
                        newL = ob.Left + dx; newW = ob.Width - dx; newH = ob.Height + dy; break;
                    case ResizeHandle.Left:
                        newL = ob.Left + dx; newW = ob.Width - dx; break;
                }
                if (newW < 4) newW = 4;
                if (newH < 4) newH = 4;
                ScaleStroke(_selectedStroke, _strokeOrigPoints!, ob, newL, newT, newW, newH);
                UpdateSelectionOverlayForStroke(_selectedStroke);
            }
            else if (_isDragging)
            {
                // Stroke move
                var dx = pos.X - _strokeDragStart.X;
                var dy = pos.Y - _strokeDragStart.Y;
                MoveStrokeTo(_selectedStroke, _strokeOrigPoints!, dx, dy);
                UpdateSelectionOverlayForStroke(_selectedStroke);
            }
            return;
        }

        if (_selectedElement is null) return;

        if (_activeHandle != ResizeHandle.None && e.LeftButton == MouseButtonState.Pressed)
        {
            ApplyResize(pos);
            UpdateSelectionOverlay();
            return;
        }

        if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
        {
            var dx = pos.X - _dragStart.X;
            var dy = pos.Y - _dragStart.Y;
            MoveElement(_selectedElement, _dragOrigLeft + dx, _dragOrigTop + dy);
            UpdateSelectionOverlay();
        }
    }

    private void SelectMouseUp(MouseButtonEventArgs e)
    {
        // Stroke drag/resize end
        if (_selectedStroke is not null && (_isDragging || _activeHandle != ResizeHandle.None))
        {
            var stroke   = _selectedStroke;
            var origPts  = _strokeOrigPoints!;
            var origB    = _strokeOrigBounds;
            var finalPts = new StylusPointCollection(stroke.StylusPoints);

            _redoStack.Clear();
            _undoStack.Push(new(
                Undo: () => { stroke.StylusPoints = new StylusPointCollection(origPts);  UpdateSelectionOverlayForStroke(stroke); },
                Redo: () => { stroke.StylusPoints = new StylusPointCollection(finalPts); UpdateSelectionOverlayForStroke(stroke); }));

            _isDragging   = false;
            _activeHandle = ResizeHandle.None;
            Mouse.Capture(null);
            return;
        }

        if (_selectedElement is null) { Mouse.Capture(null); return; }

        if (_activeHandle != ResizeHandle.None)
        {
            var el     = _selectedElement;
            var origL  = _resizeOrigLeft; var origT = _resizeOrigTop;
            var origW  = _resizeOrigW;    var origH = _resizeOrigH;
            var origP1 = _resizeOrigP1;   var origP2 = _resizeOrigP2;
            var newBounds = GetElementBounds(el);

            _redoStack.Clear();
            _undoStack.Push(new(
                Undo: () => { RestoreElementBounds(el, origL, origT, origW, origH, origP1, origP2); UpdateSelectionOverlayFor(el); },
                Redo: () => { ApplyBoundsToElement(el, newBounds.Left, newBounds.Top, newBounds.Width, newBounds.Height); UpdateSelectionOverlayFor(el); }));

            _activeHandle = ResizeHandle.None;
        }
        else if (_isDragging)
        {
            var el   = _selectedElement;
            var newB = GetElementBounds(el);
            var newL = newB.Left;
            var newT = newB.Top;

            if (Math.Abs(newL - _dragOrigLeft) > 0.5 || Math.Abs(newT - _dragOrigTop) > 0.5)
            {
                var oL = _dragOrigLeft; var oT = _dragOrigTop;
                _redoStack.Clear();
                _undoStack.Push(new(
                    Undo: () => { MoveElement(el, oL, oT);     UpdateSelectionOverlayFor(el); },
                    Redo: () => { MoveElement(el, newL, newT); UpdateSelectionOverlayFor(el); }));
            }
        }

        _isDragging = false;
        Mouse.Capture(null);
    }

    // ── Selection overlay ─────────────────────────────────────────────────────
    private void SelectElement(UIElement el)
    {
        if (_selectedElement == el) return;
        Deselect();
        _selectedElement = el;
        DrawSelectionOverlay(el);

        double? thickness = el switch
        {
            Shape s      => s.StrokeThickness,
            TextBlock    => null,
            _            => null,
        };
        SelectionThicknessChanged?.Invoke(thickness);
    }

    private void Deselect()
    {
        if (_selectedElement is null) return;
        _selectedElement = null;
        RemoveSelectionOverlay();
        Canvas.Cursor = Cursors.Arrow;
    }

    // ── Stroke selection ──────────────────────────────────────────────────────
    private void SelectStroke(Stroke stroke, Point pos)
    {
        _selectedStroke    = stroke;
        _isDragging        = true;
        _activeHandle      = ResizeHandle.None;
        _strokeDragStart   = pos;
        _strokeOrigPoints  = new StylusPointCollection(stroke.StylusPoints);
        _strokeOrigBounds  = stroke.GetBounds();
        DrawSelectionOverlayForBounds(_strokeOrigBounds);
        SelectionThicknessChanged?.Invoke(stroke.DrawingAttributes.Width);
    }

    private void DeselectStroke()
    {
        if (_selectedStroke is null) return;
        _selectedStroke   = null;
        _strokeOrigPoints = null;
        RemoveSelectionOverlay();
        Canvas.Cursor = Cursors.Arrow;
    }

    private void MoveStrokeTo(Stroke stroke, StylusPointCollection origPts, double dx, double dy)
    {
        var moved = new StylusPointCollection(
            origPts.Select(p => new StylusPoint(p.X + dx, p.Y + dy, p.PressureFactor)));
        stroke.StylusPoints = moved;
    }

    private void UpdateSelectionOverlayForStroke(Stroke stroke)
    {
        if (_selectionRect is null) return;
        var b = stroke.GetBounds();
        const double pad = 4;
        const double hs  = 10;
        InkCanvas.SetLeft(_selectionRect, b.Left - pad);
        InkCanvas.SetTop (_selectionRect, b.Top  - pad);
        _selectionRect.Width  = b.Width  + pad * 2;
        _selectionRect.Height = b.Height + pad * 2;

        if (_handles is not null)
        {
            var positions = GetHandlePositions(b, pad, hs);
            for (int i = 0; i < 8; i++)
            {
                InkCanvas.SetLeft(_handles[i], positions[i].X);
                InkCanvas.SetTop (_handles[i], positions[i].Y);
            }
        }
    }

    private void ScaleStroke(Stroke stroke, StylusPointCollection origPts, Rect origBounds,
                             double newL, double newT, double newW, double newH)
    {
        if (origBounds.Width < 1 || origBounds.Height < 1) return;
        double sx = newW / origBounds.Width;
        double sy = newH / origBounds.Height;
        var scaled = new StylusPointCollection(
            origPts.Select(p => new StylusPoint(
                newL + (p.X - origBounds.Left) * sx,
                newT + (p.Y - origBounds.Top)  * sy,
                p.PressureFactor)));
        stroke.StylusPoints = scaled;
    }

    private Stroke? HitTestStroke(Point pos)
    {
        const double HitPad = 8;
        foreach (var stroke in Canvas.Strokes)
        {
            var b = stroke.GetBounds();
            var expanded = new Rect(b.Left - HitPad, b.Top - HitPad,
                                    b.Width + HitPad * 2, b.Height + HitPad * 2);
            if (expanded.Contains(pos)) return stroke;
        }
        return null;
    }

    private void DrawSelectionOverlayForBounds(Rect b)
    {
        RemoveSelectionOverlay();
        const double pad = 4;
        const double hs  = 10;

        _selectionRect = new Rectangle
        {
            Width            = b.Width  + pad * 2,
            Height           = b.Height + pad * 2,
            Stroke           = new SolidColorBrush(Color.FromArgb(220, 80, 180, 255)),
            StrokeThickness  = 1.5,
            StrokeDashArray  = new DoubleCollection([5, 3]),
            Fill             = new SolidColorBrush(Color.FromArgb(15, 80, 180, 255)),
            IsHitTestVisible = false,
        };
        InkCanvas.SetLeft(_selectionRect, b.Left - pad);
        InkCanvas.SetTop (_selectionRect, b.Top  - pad);
        Canvas.Children.Add(_selectionRect);

        _handles = new Rectangle[8];
        var positions = GetHandlePositions(b, pad, hs);
        for (int i = 0; i < 8; i++)
        {
            var h = new Rectangle
            {
                Width            = hs,
                Height           = hs,
                Fill             = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                Stroke           = new SolidColorBrush(Color.FromArgb(200, 80, 180, 255)),
                StrokeThickness  = 1.5,
                IsHitTestVisible = false,
            };
            InkCanvas.SetLeft(h, positions[i].X);
            InkCanvas.SetTop (h, positions[i].Y);
            Canvas.Children.Add(h);
            _handles[i] = h;
        }
    }

    private void DrawSelectionOverlay(UIElement el)
    {
        var b = GetElementBounds(el);
        const double pad = 4;
        const double hs  = 10; // handle size

        // Selection border
        _selectionRect = new Rectangle
        {
            Width           = b.Width  + pad * 2,
            Height          = b.Height + pad * 2,
            Stroke          = new SolidColorBrush(Color.FromArgb(220, 80, 180, 255)),
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection([5, 3]),
            Fill            = new SolidColorBrush(Color.FromArgb(15, 80, 180, 255)),
            IsHitTestVisible = false,
        };
        InkCanvas.SetLeft(_selectionRect, b.Left - pad);
        InkCanvas.SetTop (_selectionRect, b.Top  - pad);
        Canvas.Children.Add(_selectionRect);

        // 8 handles: TL T TR R BR B BL L  (indexes 1,3,5,7 are mid-edge — hidden for TextBlock)
        bool isText = el is TextBlock;
        _handles = new Rectangle[8];
        var positions = GetHandlePositions(b, pad, hs);
        for (int i = 0; i < 8; i++)
        {
            bool isMid = i % 2 == 1; // T=1, R=3, B=5, L=7
            var h = new Rectangle
            {
                Width            = hs,
                Height           = hs,
                Fill             = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                Stroke           = new SolidColorBrush(Color.FromArgb(200, 80, 180, 255)),
                StrokeThickness  = 1.5,
                IsHitTestVisible = false,
                Visibility       = (isText && isMid) ? Visibility.Hidden : Visibility.Visible,
            };
            InkCanvas.SetLeft(h, positions[i].X);
            InkCanvas.SetTop (h, positions[i].Y);
            Canvas.Children.Add(h);
            _handles[i] = h;
        }
    }

    private void RemoveSelectionOverlay()
    {
        if (_selectionRect is not null)
        {
            Canvas.Children.Remove(_selectionRect);
            _selectionRect = null;
        }
        if (_handles is not null)
        {
            foreach (var h in _handles) Canvas.Children.Remove(h);
            _handles = null;
        }
    }

    private void UpdateSelectionOverlay()
    {
        if (_selectedElement is not null)
            UpdateSelectionOverlayFor(_selectedElement);
    }

    private void UpdateSelectionOverlayFor(UIElement el)
    {
        if (_selectedElement != el || _selectionRect is null || _handles is null) return;
        var b = GetElementBounds(el);
        const double pad = 4;
        const double hs  = 10;

        InkCanvas.SetLeft(_selectionRect, b.Left - pad);
        InkCanvas.SetTop (_selectionRect, b.Top  - pad);
        _selectionRect.Width  = b.Width  + pad * 2;
        _selectionRect.Height = b.Height + pad * 2;

        var positions = GetHandlePositions(b, pad, hs);
        for (int i = 0; i < 8; i++)
        {
            InkCanvas.SetLeft(_handles[i], positions[i].X);
            InkCanvas.SetTop (_handles[i], positions[i].Y);
        }
    }

    private static Point[] GetHandlePositions(Rect b, double pad, double hs)
    {
        double l = b.Left   - pad;
        double t = b.Top    - pad;
        double r = b.Right  + pad;
        double bm = b.Bottom + pad;
        double mx = (l + r)  / 2;
        double my = (t + bm) / 2;
        double hh = hs / 2;
        return
        [
            new(l  - hh, t  - hh), // TL
            new(mx - hh, t  - hh), // T
            new(r  - hh, t  - hh), // TR
            new(r  - hh, my - hh), // R
            new(r  - hh, bm - hh), // BR
            new(mx - hh, bm - hh), // B
            new(l  - hh, bm - hh), // BL
            new(l  - hh, my - hh), // L
        ];
    }

    // ── Resize logic ──────────────────────────────────────────────────────────
    private void CaptureResizeOrigin()
    {
        if (_selectedElement is null) return;
        var b = GetElementBounds(_selectedElement);
        _resizeOrigLeft = b.Left;
        _resizeOrigTop  = b.Top;
        _resizeOrigW    = b.Width;
        _resizeOrigH    = b.Height;

        if (_selectedElement is Line ln)
        {
            _resizeOrigP1 = new Point(ln.X1, ln.Y1);
            _resizeOrigP2 = new Point(ln.X2, ln.Y2);
        }
        else if (_selectedElement is Path ap && _arrowPoints.TryGetValue(ap, out var apts))
        {
            _resizeOrigP1 = apts.P1;
            _resizeOrigP2 = apts.P2;
        }
        else if (_selectedElement is TextBlock tb)
        {
            _resizeOrigH = tb.FontSize; // store actual font size, not rendered height
        }
    }

    private void ApplyResize(Point pos)
    {
        if (_selectedElement is null) return;

        var dx = pos.X - _dragStart.X;
        var dy = pos.Y - _dragStart.Y;

        // Line and Arrow: each handle moves the nearest endpoint
        // P1 is the endpoint closer to TL, P2 closer to BR
        if (_selectedElement is Line line)
        {
            bool p1IsTL = (_resizeOrigP1.X + _resizeOrigP1.Y) <= (_resizeOrigP2.X + _resizeOrigP2.Y);
            var (ptTL, ptBR) = p1IsTL ? (_resizeOrigP1, _resizeOrigP2) : (_resizeOrigP2, _resizeOrigP1);
            switch (_activeHandle)
            {
                case ResizeHandle.TopLeft:
                    if (p1IsTL) { line.X1 = ptTL.X + dx; line.Y1 = ptTL.Y + dy; }
                    else        { line.X2 = ptTL.X + dx; line.Y2 = ptTL.Y + dy; }
                    break;
                case ResizeHandle.TopRight:
                    // top endpoint gets dy, right endpoint gets dx
                    bool p1IsTop = _resizeOrigP1.Y <= _resizeOrigP2.Y;
                    if (p1IsTop) { line.Y1 = _resizeOrigP1.Y + dy; line.X2 = _resizeOrigP2.X + dx; }
                    else         { line.Y2 = _resizeOrigP2.Y + dy; line.X1 = _resizeOrigP1.X + dx; }
                    break;
                case ResizeHandle.BottomRight:
                    if (p1IsTL) { line.X2 = ptBR.X + dx; line.Y2 = ptBR.Y + dy; }
                    else        { line.X1 = ptBR.X + dx; line.Y1 = ptBR.Y + dy; }
                    break;
                case ResizeHandle.BottomLeft:
                    bool p1IsLeft = _resizeOrigP1.X <= _resizeOrigP2.X;
                    if (p1IsLeft) { line.X1 = _resizeOrigP1.X + dx; line.Y2 = _resizeOrigP2.Y + dy; }
                    else          { line.X2 = _resizeOrigP2.X + dx; line.Y1 = _resizeOrigP1.Y + dy; }
                    break;
                case ResizeHandle.Top:
                    if (_resizeOrigP1.Y <= _resizeOrigP2.Y) { line.Y1 = _resizeOrigP1.Y + dy; }
                    else                                    { line.Y2 = _resizeOrigP2.Y + dy; }
                    break;
                case ResizeHandle.Bottom:
                    if (_resizeOrigP1.Y >= _resizeOrigP2.Y) { line.Y1 = _resizeOrigP1.Y + dy; }
                    else                                    { line.Y2 = _resizeOrigP2.Y + dy; }
                    break;
                case ResizeHandle.Left:
                    if (_resizeOrigP1.X <= _resizeOrigP2.X) { line.X1 = _resizeOrigP1.X + dx; }
                    else                                    { line.X2 = _resizeOrigP2.X + dx; }
                    break;
                case ResizeHandle.Right:
                    if (_resizeOrigP1.X >= _resizeOrigP2.X) { line.X1 = _resizeOrigP1.X + dx; }
                    else                                    { line.X2 = _resizeOrigP2.X + dx; }
                    break;
            }
            return;
        }

        if (_selectedElement is Path arrow)
        {
            bool p1IsTL = (_resizeOrigP1.X + _resizeOrigP1.Y) <= (_resizeOrigP2.X + _resizeOrigP2.Y);
            Point np1 = _resizeOrigP1, np2 = _resizeOrigP2;
            switch (_activeHandle)
            {
                case ResizeHandle.TopLeft:
                    if (p1IsTL) np1 = new Point(_resizeOrigP1.X + dx, _resizeOrigP1.Y + dy);
                    else        np2 = new Point(_resizeOrigP2.X + dx, _resizeOrigP2.Y + dy);
                    break;
                case ResizeHandle.TopRight:
                    bool p1IsTop = _resizeOrigP1.Y <= _resizeOrigP2.Y;
                    if (p1IsTop) { np1 = new Point(np1.X, _resizeOrigP1.Y + dy); np2 = new Point(_resizeOrigP2.X + dx, np2.Y); }
                    else         { np2 = new Point(np2.X, _resizeOrigP2.Y + dy); np1 = new Point(_resizeOrigP1.X + dx, np1.Y); }
                    break;
                case ResizeHandle.BottomRight:
                    if (p1IsTL) np2 = new Point(_resizeOrigP2.X + dx, _resizeOrigP2.Y + dy);
                    else        np1 = new Point(_resizeOrigP1.X + dx, _resizeOrigP1.Y + dy);
                    break;
                case ResizeHandle.BottomLeft:
                    bool p1IsLeft = _resizeOrigP1.X <= _resizeOrigP2.X;
                    if (p1IsLeft) { np1 = new Point(_resizeOrigP1.X + dx, np1.Y); np2 = new Point(np2.X, _resizeOrigP2.Y + dy); }
                    else          { np2 = new Point(_resizeOrigP2.X + dx, np2.Y); np1 = new Point(np1.X, _resizeOrigP1.Y + dy); }
                    break;
                case ResizeHandle.Top:
                    if (_resizeOrigP1.Y <= _resizeOrigP2.Y) np1 = new Point(np1.X, _resizeOrigP1.Y + dy);
                    else                                    np2 = new Point(np2.X, _resizeOrigP2.Y + dy);
                    break;
                case ResizeHandle.Bottom:
                    if (_resizeOrigP1.Y >= _resizeOrigP2.Y) np1 = new Point(np1.X, _resizeOrigP1.Y + dy);
                    else                                    np2 = new Point(np2.X, _resizeOrigP2.Y + dy);
                    break;
                case ResizeHandle.Left:
                    if (_resizeOrigP1.X <= _resizeOrigP2.X) np1 = new Point(_resizeOrigP1.X + dx, np1.Y);
                    else                                    np2 = new Point(_resizeOrigP2.X + dx, np2.Y);
                    break;
                case ResizeHandle.Right:
                    if (_resizeOrigP1.X >= _resizeOrigP2.X) np1 = new Point(_resizeOrigP1.X + dx, np1.Y);
                    else                                    np2 = new Point(_resizeOrigP2.X + dx, np2.Y);
                    break;
            }
            _arrowPoints[arrow] = (np1, np2);
            arrow.Data = ComputeArrowGeometry(np1, np2, arrow.StrokeThickness);
            return;
        }

        // TextBlock: drag any handle to scale font size uniformly
        // TextBlock: same rect logic as Rectangle, but apply height change as FontSize
        if (_selectedElement is TextBlock tb)
        {
            double tbL = _resizeOrigLeft;
            double tbT = _resizeOrigTop;
            double tbH = _resizeOrigH;

            switch (_activeHandle)
            {
                case ResizeHandle.TopLeft:
                    tbL = _resizeOrigLeft + dx; tbT = _resizeOrigTop + dy;
                    tbH = _resizeOrigH - dy;
                    break;
                case ResizeHandle.Top:
                    tbT = _resizeOrigTop + dy; tbH = _resizeOrigH - dy;
                    break;
                case ResizeHandle.TopRight:
                    tbT = _resizeOrigTop + dy; tbH = _resizeOrigH - dy;
                    break;
                case ResizeHandle.Right:
                    tbH = _resizeOrigH + dx;
                    break;
                case ResizeHandle.BottomRight:
                    tbH = _resizeOrigH + Math.Max(dx, dy);
                    break;
                case ResizeHandle.Bottom:
                    tbH = _resizeOrigH + dy;
                    break;
                case ResizeHandle.BottomLeft:
                    tbL = _resizeOrigLeft + dx; tbH = _resizeOrigH + dy;
                    break;
                case ResizeHandle.Left:
                    tbL = _resizeOrigLeft + dx; tbH = _resizeOrigH - dx;
                    break;
            }

            tb.FontSize = Math.Max(8, tbH);
            InkCanvas.SetLeft(tb, tbL);
            InkCanvas.SetTop (tb, tbT);
            return;
        }

        // Rectangle / Ellipse / generic Shape with Width+Height
        double newL = _resizeOrigLeft;
        double newT = _resizeOrigTop;
        double newW = _resizeOrigW;
        double newH = _resizeOrigH;

        switch (_activeHandle)
        {
            case ResizeHandle.TopLeft:
                newL = _resizeOrigLeft + dx; newT = _resizeOrigTop + dy;
                newW = _resizeOrigW    - dx; newH = _resizeOrigH   - dy;
                break;
            case ResizeHandle.Top:
                newT = _resizeOrigTop + dy; newH = _resizeOrigH - dy;
                break;
            case ResizeHandle.TopRight:
                newT = _resizeOrigTop + dy; newW = _resizeOrigW + dx; newH = _resizeOrigH - dy;
                break;
            case ResizeHandle.Right:
                newW = _resizeOrigW + dx;
                break;
            case ResizeHandle.BottomRight:
                newW = _resizeOrigW + dx; newH = _resizeOrigH + dy;
                break;
            case ResizeHandle.Bottom:
                newH = _resizeOrigH + dy;
                break;
            case ResizeHandle.BottomLeft:
                newL = _resizeOrigLeft + dx; newW = _resizeOrigW - dx; newH = _resizeOrigH + dy;
                break;
            case ResizeHandle.Left:
                newL = _resizeOrigLeft + dx; newW = _resizeOrigW - dx;
                break;
        }

        if (newW < 4) { newW = 4; }
        if (newH < 4) { newH = 4; }
        ApplyBoundsToElement(_selectedElement, newL, newT, newW, newH);
    }

    private static void ApplyBoundsToElement(UIElement el, double l, double t, double w, double h)
    {
        InkCanvas.SetLeft(el, l);
        InkCanvas.SetTop (el, t);
        switch (el)
        {
            case Rectangle r:  r.Width = w; r.Height = h; break;
            case Ellipse   e:  e.Width = w; e.Height = h; break;
        }
    }

    private void RestoreElementBounds(UIElement el,
        double origL, double origT, double origW, double origH,
        Point origP1, Point origP2)
    {
        if (el is Line ln)
        {
            ln.X1 = origP1.X; ln.Y1 = origP1.Y;
            ln.X2 = origP2.X; ln.Y2 = origP2.Y;
            return;
        }
        if (el is Path arrow)
        {
            _arrowPoints[arrow] = (origP1, origP2);
            arrow.Data = ComputeArrowGeometry(origP1, origP2, arrow.StrokeThickness);
            return;
        }
        ApplyBoundsToElement(el, origL, origT, origW, origH);
        if (el is TextBlock tb)
            tb.FontSize = Math.Max(8, origH * 0.8);
    }

    // ── Element bounds ────────────────────────────────────────────────────────
    private Rect GetElementBounds(UIElement el)
    {
        double l = InkCanvas.GetLeft(el);
        double t = InkCanvas.GetTop(el);
        if (double.IsNaN(l)) l = 0;
        if (double.IsNaN(t)) t = 0;

        switch (el)
        {
            case Line ln:
                var lx = Math.Min(ln.X1, ln.X2);
                var ly = Math.Min(ln.Y1, ln.Y2);
                var lw = Math.Abs(ln.X2 - ln.X1);
                var lh = Math.Abs(ln.Y2 - ln.Y1);
                return new Rect(lx, ly, Math.Max(lw, 2), Math.Max(lh, 2));

            case Path p:
                var pb = p.Data?.Bounds ?? Rect.Empty;
                return pb.IsEmpty ? new Rect(l, t, 10, 10) : pb;

            default:
                el.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                return new Rect(l, t,
                    Math.Max(el.DesiredSize.Width,  4),
                    Math.Max(el.DesiredSize.Height, 4));
        }
    }

    // ── Move element ──────────────────────────────────────────────────────────
    private void MoveElement(UIElement el, double l, double t)
    {
        if (el is Line ln)
        {
            var dx = l - Math.Min(ln.X1, ln.X2);
            var dy = t - Math.Min(ln.Y1, ln.Y2);
            ln.X1 += dx; ln.Y1 += dy;
            ln.X2 += dx; ln.Y2 += dy;
            return;
        }
        if (el is Path arrow && _arrowPoints.TryGetValue(arrow, out var pts))
        {
            var pb = arrow.Data?.Bounds ?? Rect.Empty;
            if (!pb.IsEmpty)
            {
                var dx  = l - pb.Left;
                var dy  = t - pb.Top;
                var np1 = new Point(pts.P1.X + dx, pts.P1.Y + dy);
                var np2 = new Point(pts.P2.X + dx, pts.P2.Y + dy);
                _arrowPoints[arrow] = (np1, np2);
                arrow.Data = ComputeArrowGeometry(np1, np2, arrow.StrokeThickness);
            }
            return;
        }
        InkCanvas.SetLeft(el, l);
        InkCanvas.SetTop (el, t);
    }

    // ── Hit testing ───────────────────────────────────────────────────────────
    private UIElement? HitTestChild(Point pos)
    {
        const double HitPad = 8;
        foreach (UIElement child in Canvas.Children)
        {
            // Skip overlay elements
            if (child == _selectionRect || child == _hoverRect) continue;
            if (_handles is not null && Array.IndexOf(_handles, child) >= 0) continue;

            var b = GetElementBounds(child);
            var expanded = new Rect(b.Left - HitPad, b.Top - HitPad,
                                    b.Width + HitPad * 2, b.Height + HitPad * 2);
            if (expanded.Contains(pos)) return child;
        }
        return null;
    }

    private void EraseChildAt(Point pos)
    {
        const double HitRadius = 20;
        UIElement? hit = null;
        foreach (UIElement child in Canvas.Children)
        {
            if (child == _selectionRect || child == _hoverRect) continue;
            if (_handles is not null && Array.IndexOf(_handles, child) >= 0) continue;

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
        _undoStack.Push(new(
            Undo: () => Canvas.Children.Add(removed),
            Redo: () => Canvas.Children.Remove(removed)));
    }

    private void EraseAt(Point pos)
    {
        // Try UIElement children first
        const double HitPad = 20;
        UIElement? hitEl = null;
        foreach (UIElement child in Canvas.Children)
        {
            if (child == _selectionRect || child == _hoverRect) continue;
            if (_handles is not null && Array.IndexOf(_handles, child) >= 0) continue;
            var b = GetElementBounds(child);
            var expanded = new Rect(b.Left - HitPad, b.Top - HitPad,
                                    b.Width + HitPad * 2, b.Height + HitPad * 2);
            if (expanded.Contains(pos)) { hitEl = child; break; }
        }
        if (hitEl is not null)
        {
            HideHoverRect();
            Canvas.Children.Remove(hitEl);
            var removed = hitEl;
            _undoStack.Push(new(
                Undo: () => Canvas.Children.Add(removed),
                Redo: () => Canvas.Children.Remove(removed)));
            return;
        }

        // Try strokes
        var hitStroke = HitTestStroke(pos);
        if (hitStroke is not null)
        {
            HideHoverRect();
            Canvas.Strokes.Remove(hitStroke);
            var s = hitStroke;
            _undoStack.Push(new(
                Undo: () => Canvas.Strokes.Add(s),
                Redo: () => Canvas.Strokes.Remove(s)));
        }
    }

    // ── Text editing ──────────────────────────────────────────────────────────
    private void EditTextBlock(TextBlock block)
    {
        var oldText     = block.Text;
        var oldFontSize = block.FontSize;
        var pos         = new Point(InkCanvas.GetLeft(block), InkCanvas.GetTop(block));

        // Hide the TextBlock while editing
        block.Visibility = Visibility.Hidden;
        Deselect();

        var tb = new TextBox
        {
            Text            = oldText,
            Background      = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
            Foreground      = block.Foreground,
            CaretBrush      = block.Foreground,
            BorderBrush     = new SolidColorBrush(Color.FromArgb(120,
                ((SolidColorBrush)block.Foreground).Color.R,
                ((SolidColorBrush)block.Foreground).Color.G,
                ((SolidColorBrush)block.Foreground).Color.B)),
            BorderThickness = new Thickness(1),
            FontFamily      = block.FontFamily,
            FontSize        = block.FontSize,
            MinWidth        = 120,
            AcceptsReturn   = true,
            TextWrapping    = TextWrapping.Wrap,
            MaxWidth        = 600,
        };
        tb.Select(tb.Text.Length, 0); // cursor at end

        var screenPt = Canvas.PointToScreen(pos);
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
            Content            = tb,
        };

        void Commit()
        {
            if (_activeInputWindow != win) return;
            _activeInputWindow = null;
            var newText = tb.Text.Trim();
            win.Close();
            block.Visibility = Visibility.Visible;

            if (string.IsNullOrEmpty(newText))
            {
                // Remove the block if text cleared
                Canvas.Children.Remove(block);
                var removed = block;
                _redoStack.Clear();
                _undoStack.Push(new(
                    Undo: () => Canvas.Children.Add(removed),
                    Redo: () => Canvas.Children.Remove(removed)));
                return;
            }

            if (newText == oldText && block.FontSize == oldFontSize) return;

            var capturedText = newText;
            var capturedSize = block.FontSize;
            _redoStack.Clear();
            _undoStack.Push(new(
                Undo: () => { block.Text = oldText;       block.FontSize = oldFontSize; },
                Redo: () => { block.Text = capturedText;  block.FontSize = capturedSize; }));

            block.Text = newText;
        }

        _activeInputWindow = win;
        tb.LostFocus += (_, _) => Dispatcher.BeginInvoke(Commit);
        tb.KeyDown   += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                _activeInputWindow = null;
                win.Close();
                block.Visibility = Visibility.Visible;
                e.Handled = true;
            }
        };

        win.Show();
        tb.Focus();
    }

    // ── Shape builders ────────────────────────────────────────────────────────
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
            DrawingTool.Arrow => BuildArrow(a, b, brush, t),
            _ => throw new InvalidOperationException(),
        };
    }

    private Path BuildArrow(Point a, Point b, SolidColorBrush brush, double t)
    {
        var arrow = new Path
        {
            Data            = ComputeArrowGeometry(a, b, t),
            Stroke          = brush,
            Fill            = brush,
            StrokeThickness = t,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap   = PenLineCap.Round,
        };
        _arrowPoints[arrow] = (a, b);
        return arrow;
    }

    private void UpdateShape(Shape shape, Point a, Point b)
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
            case Path p:
                _arrowPoints[p] = (a, b);
                p.Data = ComputeArrowGeometry(a, b, p.StrokeThickness);
                break;
        }
    }

    private static Geometry ComputeArrowGeometry(Point a, Point b, double thickness)
    {
        var dx  = b.X - a.X;
        var dy  = b.Y - a.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 2) return new LineGeometry(a, b);

        var nx = dx / len;
        var ny = dy / len;
        var px = -ny;
        var py = nx;

        var arrowLen = Math.Min(Math.Max(thickness * 5, 20.0), len * 0.45);
        var arrowWid = Math.Min(Math.Max(thickness * 2.5, 10.0), arrowLen * 0.6);

        var shaftEnd = new Point(b.X - nx * arrowLen, b.Y - ny * arrowLen);

        var tip   = b;
        var base1 = new Point(shaftEnd.X + px * arrowWid, shaftEnd.Y + py * arrowWid);
        var base2 = new Point(shaftEnd.X - px * arrowWid, shaftEnd.Y - py * arrowWid);

        var group = new GeometryGroup { FillRule = FillRule.Nonzero };
        group.Children.Add(new LineGeometry(a, shaftEnd));

        var head = new PathGeometry();
        var fig  = new PathFigure { StartPoint = tip, IsClosed = true, IsFilled = true };
        fig.Segments.Add(new LineSegment(base1, true));
        fig.Segments.Add(new LineSegment(base2, true));
        head.Figures.Add(fig);
        group.Children.Add(head);

        return group;
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
    private void PlaceTextBox(Point canvasPos)
    {
        CommitTextBox();

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
        Deselect();
        DeselectStroke();
        HideHoverRect();
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
