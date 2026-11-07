using System.Windows;
using MessageBox = System.Windows.MessageBox;
using ScreenWriter.Models;
using ScreenWriter.Services;
using ScreenWriter.Windows;
using WpfColor          = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace ScreenWriter;

public partial class App : System.Windows.Application
{
    private const string MutexName = "Global\\ScreenWriter_SingleInstance_A3F2B1C4";
    private static Mutex? _mutex;
    private static bool _ownsMutex;

    private OverlayWindow? _overlay;
    private ToolbarWindow? _toolbar;
    private TrayService? _tray;
    private HotkeyService? _hotkeys;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, ex) =>
        {
            var sb = new System.Text.StringBuilder();
            var e = ex.Exception;
            int depth = 0;
            while (e != null && depth++ < 5)
            {
                sb.AppendLine($"[{depth}] {e.GetType().Name}: {e.Message}");
                e = e.InnerException;
            }
            System.Windows.MessageBox.Show(sb.ToString(),
                "Screen Writer – Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
            Shutdown();
        };

        _mutex = new Mutex(true, MutexName, out bool createdNew);
        _ownsMutex = createdNew;
        if (!createdNew)
        {
            LocalizationService.Instance.Load();
            var svc = LocalizationService.Instance;
            System.Windows.MessageBox.Show(
                $"{svc.Get("Str_AlreadyRunning")}\n{svc.Get("Str_CheckTray")}",
                "Screen Writer",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        LocalizationService.Instance.Load();

        base.OnStartup(e);

        var splash = new SplashWindow();
        splash.Closed += (_, _) => ShowMainWindows();
        splash.Show();
    }

    private void ShowMainWindows()
    {
        _overlay = new OverlayWindow();
        _toolbar = new ToolbarWindow();

        _toolbar.DrawingModeToggleRequested += () =>
        {
            _overlay.ToggleDrawingMode();
            _toolbar.SyncMode(_overlay.IsDrawingMode);
            _toolbar.BringToTopmost();
        };
        _toolbar.ColorChanged   += color  =>
        {
            _overlay.SetColor(color);
            // Only the screen (transparent) color is remembered between runs;
            // board colors live in memory for the session.
            if (_overlay.Mode == BoardMode.Transparent)
                SettingsService.Instance.SetColor(color.ToString());
        };
        _toolbar.PenSizeChanged += size   =>
        {
            _overlay.SetPenSize(size);
            SettingsService.Instance.SetPenSize(size);
        };
        _toolbar.ToolChanged    += tool   =>
        {
            _overlay.SetTool(tool);
            if (tool is not DrawingTool.Eraser)
                SettingsService.Instance.SetTool(tool);
        };
        _toolbar.EraserToggled  += eraser => _overlay.SetEraser(eraser);
        _overlay.SelectionThicknessChanged += thickness =>
        {
            if (thickness.HasValue)
                _toolbar.RestoreInitialPenSize(thickness.Value);
        };
        _toolbar.UndoRequested  += ()     => _overlay.Undo();
        _toolbar.RedoRequested  += ()     => _overlay.Redo();
        _toolbar.ClearRequested += ()     => _overlay.ConfirmedClearAll();
        _toolbar.ExitRequested  += ()     => RequestExit(alwaysConfirm: true);
        _toolbar.BoardModeRequested += SetBoardMode;
        _overlay.BoardModeChanged   += mode =>
        {
            _toolbar.SyncBoardMode(mode, _overlay.CurrentColor);
            _toolbar.SyncMode(_overlay.IsDrawingMode);
            _toolbar.BringToTopmost();
        };
        _toolbar.AboutRequested += ()     => new AboutWindow().Show();
        _toolbar.LangToggleRequested += () => LocalizationService.Instance.Switch();
        _toolbar.PositionToggleRequested += () =>
        {
            var onRight = !SettingsService.Instance.ToolbarOnRight;
            SettingsService.Instance.SetToolbarOnRight(onRight);
            _toolbar.ApplyPosition(onRight);
        };

        _tray    = new TrayService(_overlay, _toolbar, () => RequestExit(alwaysConfirm: false));
        _hotkeys = new HotkeyService(_overlay, _toolbar, mode =>
            // Pressing the shortcut of the current board returns to the screen.
            SetBoardMode(mode == _overlay.Mode ? BoardMode.Transparent : mode));

        _overlay.Show();
        _toolbar.Show();

        // Restore last saved settings
        try
        {
            var savedColor = (WpfColor)WpfColorConverter.ConvertFromString(SettingsService.Instance.LastColorHex);
            _overlay.SetColor(savedColor);
        }
        catch { }
        _toolbar.RestoreInitialPenSize(SettingsService.Instance.LastPenSize);
        _toolbar.RestoreInitialTool(SettingsService.Instance.LastTool);

        if (!UpdateService.IsPackagedApp)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                var info = await UpdateService.Instance.CheckAsync(CancellationToken.None);
                if (info != null)
                    Dispatcher.Invoke(() => UpdateDialog.Show(info));
            });
        }
    }

    private void SetBoardMode(BoardMode mode)
    {
        if (_overlay is null) return;
        _overlay.SetBoardMode(mode);   // raises BoardModeChanged → syncs the toolbar
    }

    /// <summary>
    /// Asks before exiting. Board drawings are not saved, so warn when a board has content;
    /// otherwise ask only when <paramref name="alwaysConfirm"/> is set (toolbar ✕ button).
    /// </summary>
    private void RequestExit(bool alwaysConfirm)
    {
        var svc = LocalizationService.Instance;
        string? msg = _overlay?.HasBoardContent == true ? svc.Get("Str_ExitBoardWarningMsg")
                    : alwaysConfirm                    ? svc.Get("Str_CloseConfirmMsg")
                    : null;
        if (msg is not null)
        {
            // Owned by a topmost window so the dialog shows above an opaque board.
            Window? owner = _toolbar is { IsVisible: true } ? _toolbar : _overlay;
            var result = owner is not null
                ? MessageBox.Show(owner, msg, svc.Get("Str_CloseConfirmTitle"),
                      MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
                : MessageBox.Show(msg, svc.Get("Str_CloseConfirmTitle"),
                      MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result != MessageBoxResult.Yes) return;
        }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
        _tray?.Dispose();
        SettingsService.Instance.Save();
        if (_ownsMutex) _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
