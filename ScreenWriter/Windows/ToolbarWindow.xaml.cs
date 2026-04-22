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

    // ─────────────────────────────────────────────────────────────────────────
    public ToolbarWindow()
    {
        InitializeComponent();

        LocalizationService.Instance.LanguageChanged += () => SyncMode(_currentModeIsDrawing);

        _slideTimer.Tick     += OnSlide;
        _hideDelayTimer.Tick += (_, _) => { _hideDelayTimer.Stop(); SlideTo(_hiddenTop); };

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
        _hideDelayTimer.Start();
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
            _                     => BtnPen,
        };
        SetActiveTool(tool, btn);
    }

    public void RestoreInitialPenSize(double size)
        => SizeSlider.Value = size;

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
            img = new BitmapImage(new Uri($"pack://application:,,,/Assets/Icons/{name}.png"));
            _iconCache[name] = img;
        }
        return img;
    }

    // ── Slider ────────────────────────────────────────────────────────────────
    private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        => PenSizeChanged?.Invoke(e.NewValue);

    // ── Undo / Redo / Clear / Close / About / Lang ────────────────────────────
    private void BtnUndo_Click    (object sender, RoutedEventArgs e) => UndoRequested?.Invoke();
    private void BtnRedo_Click    (object sender, RoutedEventArgs e) => RedoRequested?.Invoke();
    private void BtnClear_Click   (object sender, RoutedEventArgs e) => ClearRequested?.Invoke();
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        var svc    = LocalizationService.Instance;
        var result = System.Windows.MessageBox.Show(
            svc.Get("Str_CloseConfirmMsg"),
            svc.Get("Str_CloseConfirmTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (result == MessageBoxResult.Yes) ExitRequested?.Invoke();
    }
    private void BtnAbout_Click   (object sender, RoutedEventArgs e) => AboutRequested?.Invoke();
    private void BtnLang_Click    (object sender, RoutedEventArgs e) => LangToggleRequested?.Invoke();
    private void BtnPosition_Click(object sender, RoutedEventArgs e) => PositionToggleRequested?.Invoke();
}
