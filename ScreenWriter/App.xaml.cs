using System.Windows;
using ScreenWriter.Services;
using ScreenWriter.Windows;

namespace ScreenWriter;

public partial class App : System.Windows.Application
{
    private OverlayWindow? _overlay;
    private ToolbarWindow? _toolbar;
    private TrayService? _tray;
    private HotkeyService? _hotkeys;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _overlay = new OverlayWindow();
        _toolbar = new ToolbarWindow();

        _toolbar.DrawingModeToggleRequested += () =>
        {
            _overlay.ToggleDrawingMode();
            _toolbar.SyncMode(_overlay.IsDrawingMode);
            _toolbar.BringToTopmost();   // toolbar always above overlay
        };
        _toolbar.ColorChanged += color => _overlay.SetColor(color);
        _toolbar.PenSizeChanged += size => _overlay.SetPenSize(size);
        _toolbar.ToolChanged   += tool   => _overlay.SetTool(tool);
        _toolbar.EraserToggled += eraser => _overlay.SetEraser(eraser);
        _toolbar.UndoRequested += () => _overlay.Undo();
        _toolbar.RedoRequested += () => _overlay.Redo();
        _toolbar.ClearRequested += () => _overlay.ConfirmedClearAll();

        _tray = new TrayService(_overlay, _toolbar);
        _hotkeys = new HotkeyService(_overlay, _toolbar);

        _overlay.Show();
        _toolbar.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
        _tray?.Dispose();
        base.OnExit(e);
    }
}
