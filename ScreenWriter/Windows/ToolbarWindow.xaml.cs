using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color          = System.Windows.Media.Color;
using Brushes        = System.Windows.Media.Brushes;
using ColorConverter = System.Windows.Media.ColorConverter;
using Button         = System.Windows.Controls.Button;
using Image          = System.Windows.Controls.Image;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

using ScreenWriter.Models;
using ScreenWriter.Services;

namespace ScreenWriter.Windows;

public partial class ToolbarWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private static readonly nint HWND_TOPMOST = new(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<Color>?       ColorChanged;
    public event Action<double>?      PenSizeChanged;
    public event Action<bool>?        EraserToggled;
    public event Action?              UndoRequested;
    public event Action?              RedoRequested;
    public event Action?              ClearRequested;
    public event Action?              DrawingModeToggleRequested;
    public event Action<DrawingTool>? ToolChanged;
    public event Action?              ExitRequested;
    public event Action?              AboutRequested;
    public event Action?              LangToggleRequested;
    public event Action?              PositionToggleRequested;
    public event Action<BoardMode>?   BoardModeRequested;
    public event Action<BoardCommand>? FileCommandRequested;

    // ── Slide animation ───────────────────────────────────────────────────────
    private const double TriggerHeight = 4;
    private double _hiddenTop;
    private double _targetTop;

    private readonly DispatcherTimer _slideTimer     = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly DispatcherTimer _hideDelayTimer = new() { Interval = TimeSpan.FromSeconds(2.5) };

    // ── Visual state ──────────────────────────────────────────────────────────
    private static readonly SolidColorBrush ActiveBrush   = new(Color.FromRgb(6, 214, 160));
    private static readonly SolidColorBrush InactiveBrush = new(Color.FromRgb(0xCC, 0xCC, 0xCC));

    private static readonly Dictionary<string, BitmapImage> _iconCache = [];

    private Dictionary<Button, string> _btnIcons = [];

    private Button? _activeToolBtn;
    private bool    _eraserActive;
    private bool    _currentModeIsDrawing;

    // Auto-hide works on the transparent screen only; a board keeps the toolbar shown.
    private bool     _autoHide = true;
    private DateTime _boardPopupClosedAt;
    private DateTime _filePopupClosedAt;
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(2) };

    // ─────────────────────────────────────────────────────────────────────────
    public ToolbarWindow()
    {
        InitializeComponent();

        LocalizationService.Instance.LanguageChanged += () =>
        {
            SyncMode(_currentModeIsDrawing);
            BoardMenu.FlowDirection = LocalizationService.Instance.FlowDirection;
            FileMenu.FlowDirection  = LocalizationService.Instance.FlowDirection;
        };
        BoardMenu.FlowDirection = LocalizationService.Instance.FlowDirection;
        FileMenu.FlowDirection  = LocalizationService.Instance.FlowDirection;

        // The File button appears on boards: keep a right-aligned toolbar on the screen.
        SizeChanged += (_, _) => ApplyPosition(SettingsService.Instance.ToolbarOnRight);

        _statusTimer.Tick += (_, _) => { _statusTimer.Stop(); SyncMode(_currentModeIsDrawing); };

        // The toolbar, its menu and its tooltips are hidden from screen sharing.
        SourceInitialized += (_, _) => CaptureExclusion.Apply(new WindowInteropHelper(this).Handle);

        _slideTimer.Tick     += OnSlide;
        _hideDelayTimer.Tick += (_, _) =>
        {
            _hideDelayTimer.Stop();
            if (_autoHide && !BoardPopup.IsOpen) SlideTo(_hiddenTop);
        };

        Loaded += (_, _) =>
        {
            _btnIcons = new()
            {
                { BtnPen,     "pen"     },
                { BtnArrow,   "arrow"   },
                { BtnLine,    "line"    },
                { BtnRect,    "rect"    },
                { BtnEllipse, "ellipse" },
                { BtnText,    "text"    },
                { BtnSelect,  "select"  },
                { BtnEraser,  "eraser"  },
            };
            _hiddenTop = -(ActualHeight - TriggerHeight);
            SetActiveTool(DrawingTool.Pen, BtnPen);
            BringToTopmost();
            ApplyPosition(SettingsService.Instance.ToolbarOnRight);
            _hideDelayTimer.Start();
        };
    }

    // ── Slide helpers ─────────────────────────────────────────────────────────
    private void SlideTo(double target)
    {
        _targetTop = target;
        _slideTimer.Start();
    }

    private void OnSlide(object? sender, EventArgs e)
    {
        var diff = _targetTop - Top;
        if (Math.Abs(diff) < 0.5) { Top = _targetTop; _slideTimer.Stop(); }
        else                       { Top += diff * 0.25; }
    }

    private void Toolbar_MouseEnter(object sender, MouseEventArgs e)
    {
        _hideDelayTimer.Stop();
        SlideTo(0);
    }

    private void Toolbar_MouseLeave(object sender, MouseEventArgs e)
    {
        _hideDelayTimer.Stop();
        if (_autoHide) _hideDelayTimer.Start();
    }

    // ── Board mode menu ───────────────────────────────────────────────────────
    private void BtnBoard_Click(object sender, RoutedEventArgs e)
    {
        // A click on the button while the menu is open first closes it (StaysOpen=False);
        // don't reopen it straight away.
        if ((DateTime.Now - _boardPopupClosedAt).TotalMilliseconds < 250) return;
        BoardPopup.IsOpen = !BoardPopup.IsOpen;
    }

    private void BoardPopup_Opened(object? sender, EventArgs e)
    {
        _hideDelayTimer.Stop();
        CaptureExclusion.Apply(BoardPopup.Child);
    }

    private void BoardPopup_Closed(object? sender, EventArgs e)
    {
        _boardPopupClosedAt = DateTime.Now;
        if (_autoHide && !IsMouseOver) _hideDelayTimer.Start();
    }

    private void BoardItem_Click(object sender, RoutedEventArgs e)
    {
        BoardPopup.IsOpen = false;
        if (sender is Button { Tag: string tag } && Enum.TryParse<BoardMode>(tag, out var mode))
            BoardModeRequested?.Invoke(mode);
    }

    // ── File menu ─────────────────────────────────────────────────────────────
    private void BtnFile_Click(object sender, RoutedEventArgs e)
    {
        if ((DateTime.Now - _filePopupClosedAt).TotalMilliseconds < 250) return;
        FilePopup.IsOpen = !FilePopup.IsOpen;
    }

    private void FilePopup_Opened(object? sender, EventArgs e) => CaptureExclusion.Apply(FilePopup.Child);

    private void FilePopup_Closed(object? sender, EventArgs e) => _filePopupClosedAt = DateTime.Now;

    private void FileItem_Click(object sender, RoutedEventArgs e)
    {
        FilePopup.IsOpen = false;
        if (sender is Button { Tag: string tag } && Enum.TryParse<BoardCommand>(tag, out var command))
            FileCommandRequested?.Invoke(command);
    }

    public void CloseMenus()
    {
        BoardPopup.IsOpen = false;
        FilePopup.IsOpen  = false;
    }

    /// <summary>Shows a short confirmation in place of the mode text.</summary>
    public void FlashStatus(string text)
    {
        _statusTimer.Stop();
        ModeText.Text       = text;
        ModeText.Foreground = ActiveBrush;
        _statusTimer.Start();
    }

    // Tooltips open in their own popup windows; hide the toolbar's tooltips from capture too.
    static ToolbarWindow()
    {
        EventManager.RegisterClassHandler(typeof(System.Windows.Controls.ToolTip),
            System.Windows.Controls.ToolTip.OpenedEvent,
            new RoutedEventHandler((s, _) =>
            {
                if (s is System.Windows.Controls.ToolTip { PlacementTarget: { } target }
                    && Window.GetWindow(target) is ToolbarWindow)
                    CaptureExclusion.Apply((System.Windows.Controls.ToolTip)s);
            }));
    }

    /// <summary>Updates the board icon, menu check marks, color swatches and auto-hide.</summary>
    public void SyncBoardMode(BoardMode mode, Color activeColor)
    {
        BoardIcon.Background = mode switch
        {
            BoardMode.White => Brushes.White,
            BoardMode.Black => new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x12)),
            _               => Brushes.Transparent,
        };
        BoardCheckTransparent.Visibility = mode == BoardMode.Transparent ? Visibility.Visible : Visibility.Hidden;
        BoardCheckWhite.Visibility       = mode == BoardMode.White       ? Visibility.Visible : Visibility.Hidden;
        BoardCheckBlack.Visibility       = mode == BoardMode.Black       ? Visibility.Visible : Visibility.Hidden;

        // Swap the palette for colors that are visible on this background.
        var palette = DrawingSettings.GetPalette(mode);
        var buttons = ColorPanel.Children.OfType<Button>().ToList();
        if (_activeColorBtn is not null) _activeColorBtn.BorderBrush = Brushes.Transparent;
        _activeColorBtn = null;
        for (int i = 0; i < buttons.Count && i < palette.Length; i++)
        {
            var c = (Color)ColorConverter.ConvertFromString(palette[i]);
            buttons[i].Tag        = palette[i];
            buttons[i].Background = new SolidColorBrush(c);
            if (c == activeColor)
            {
                _activeColorBtn = buttons[i];
                buttons[i].BorderBrush = Brushes.White;
            }
        }

        BtnFile.Visibility = mode == BoardMode.Transparent ? Visibility.Collapsed : Visibility.Visible;
        if (mode == BoardMode.Transparent) FilePopup.IsOpen = false;

        _autoHide = mode == BoardMode.Transparent;
        if (_autoHide)
        {
            if (!IsMouseOver && !BoardPopup.IsOpen) { _hideDelayTimer.Stop(); _hideDelayTimer.Start(); }
        }
        else
        {
            _hideDelayTimer.Stop();
            SlideTo(0);
        }
    }

    // ── Topmost ───────────────────────────────────────────────────────────────
    public void BringToTopmost()
    {
        var hwnd = new WindowInteropHelper(this).EnsureHandle();
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }

    // ── Position toggle ───────────────────────────────────────────────────────
    public void ApplyPosition(bool onRight)
    {
        if (onRight)
        {
            Left = SystemParameters.PrimaryScreenWidth - ActualWidth;
            ToolbarBorder.CornerRadius = new CornerRadius(0, 0, 0, 12);
        }
        else
        {
            Left = 0;
            ToolbarBorder.CornerRadius = new CornerRadius(0, 0, 12, 0);
        }
    }

    // ── Restore saved settings ────────────────────────────────────────────────
    public void RestoreInitialTool(DrawingTool tool)
    {
        var btn = tool switch
        {
            DrawingTool.Arrow     => BtnArrow,
            DrawingTool.Line      => BtnLine,
            DrawingTool.Rectangle => BtnRect,
            DrawingTool.Ellipse   => BtnEllipse,
            DrawingTool.Text      => BtnText,
            DrawingTool.Select    => BtnSelect,
            _                     => BtnPen,
        };
        SetActiveTool(tool, btn);
    }

    private bool _suppressPenSizeEvent;

    public void RestoreInitialPenSize(double size)
    {
        _suppressPenSizeEvent = true;
        SizeSlider.Value = size;
        _suppressPenSizeEvent = false;
    }

    // ── Mode indicator ────────────────────────────────────────────────────────
    private void BtnToggleMode_Click(object sender, RoutedEventArgs e)
        => DrawingModeToggleRequested?.Invoke();

    public void SyncMode(bool isDrawing)
    {
        _currentModeIsDrawing   = isDrawing;
        var svc                 = LocalizationService.Instance;
        ModeIndicator.Fill      = isDrawing ? ActiveBrush : new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
        ModeText.Text           = isDrawing ? svc.Get("Str_ModeActive") : svc.Get("Str_ModeInactive");
        ModeText.Foreground     = isDrawing ? ActiveBrush : InactiveBrush;
    }

    // ── Color palette ─────────────────────────────────────────────────────────
    private void ColorBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        if (_activeColorBtn is not null) _activeColorBtn.BorderBrush = Brushes.Transparent;
        _activeColorBtn = btn;
        btn.BorderBrush = Brushes.White;

        if (_eraserActive) SetActiveTool(DrawingTool.Pen, BtnPen);

        var color = (Color)ColorConverter.ConvertFromString(btn.Tag as string ?? "#E63946");
        ColorChanged?.Invoke(color);
    }

    private Button? _activeColorBtn;

    // ── Tool buttons ──────────────────────────────────────────────────────────
    private void BtnPen_Click    (object sender, RoutedEventArgs e) => SetActiveTool(DrawingTool.Pen,       BtnPen);
    private void BtnArrow_Click  (object sender, RoutedEventArgs e) => SetActiveTool(DrawingTool.Arrow,     BtnArrow);
    private void BtnLine_Click   (object sender, RoutedEventArgs e) => SetActiveTool(DrawingTool.Line,      BtnLine);
    private void BtnRect_Click   (object sender, RoutedEventArgs e) => SetActiveTool(DrawingTool.Rectangle, BtnRect);
    private void BtnEllipse_Click(object sender, RoutedEventArgs e) => SetActiveTool(DrawingTool.Ellipse,   BtnEllipse);
    private void BtnText_Click   (object sender, RoutedEventArgs e) => SetActiveTool(DrawingTool.Text,      BtnText);

    private void BtnSelect_Click (object sender, RoutedEventArgs e) => SetActiveTool(DrawingTool.Select, BtnSelect);

    private void BtnEraser_Click(object sender, RoutedEventArgs e)
    {
        _eraserActive = !_eraserActive;
        if (_eraserActive)
        {
            if (_activeToolBtn is not null) RestoreIcon(_activeToolBtn);
            SetBtnIcon(BtnEraser, "eraser-red");
            _activeToolBtn = BtnEraser;
            ToolChanged?.Invoke(DrawingTool.Eraser);
            EraserToggled?.Invoke(true);
        }
        else
        {
            SetActiveTool(DrawingTool.Pen, BtnPen);
            EraserToggled?.Invoke(false);
        }
    }

    private void SetActiveTool(DrawingTool tool, Button btn)
    {
        _eraserActive = false;
        if (_activeToolBtn is not null) RestoreIcon(_activeToolBtn);
        _activeToolBtn = btn;
        if (_btnIcons.TryGetValue(btn, out var icon))
            SetBtnIcon(btn, icon + "-red");
        ToolChanged?.Invoke(tool);
    }

    private void RestoreIcon(Button btn)
    {
        if (_btnIcons.TryGetValue(btn, out var icon))
            SetBtnIcon(btn, icon);
    }

    private static void SetBtnIcon(Button btn, string iconName)
    {
        if (btn.Content is Image img)
            img.Source = GetIcon(iconName);
    }

    private static BitmapImage GetIcon(string name)
    {
        if (!_iconCache.TryGetValue(name, out var img))
        {
            img = new BitmapImage();
            img.BeginInit();
            img.UriSource      = new Uri($"pack://application:,,,/Assets/Icons/{name}.png");
            img.CacheOption    = BitmapCacheOption.OnLoad;
            img.DecodePixelWidth = 36;
            img.EndInit();
            img.Freeze();
            _iconCache[name] = img;
        }
        return img;
    }

    // ── Slider ────────────────────────────────────────────────────────────────
    private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_suppressPenSizeEvent)
            PenSizeChanged?.Invoke(e.NewValue);
    }

    // ── Undo / Redo / Clear / Close / About / Lang ────────────────────────────
    private void BtnUndo_Click    (object sender, RoutedEventArgs e) => UndoRequested?.Invoke();
    private void BtnRedo_Click    (object sender, RoutedEventArgs e) => RedoRequested?.Invoke();
    private void BtnClear_Click   (object sender, RoutedEventArgs e) => ClearRequested?.Invoke();
    // The confirmation dialog is shown by App.RequestExit.
    private void BtnClose_Click(object sender, RoutedEventArgs e) => ExitRequested?.Invoke();
    private void BtnAbout_Click   (object sender, RoutedEventArgs e) => AboutRequested?.Invoke();
    private void BtnLang_Click    (object sender, RoutedEventArgs e) => LangToggleRequested?.Invoke();
    private void BtnPosition_Click(object sender, RoutedEventArgs e) => PositionToggleRequested?.Invoke();
}
