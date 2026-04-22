using System.Windows;
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
        _toolbar.UndoRequested  += ()     => _overlay.Undo();
        _toolbar.RedoRequested  += ()     => _overlay.Redo();
        _toolbar.ClearRequested += ()     => _overlay.ConfirmedClearAll();
        _toolbar.ExitRequested  += ()     => Shutdown();
        _toolbar.AboutRequested += ()     => new AboutWindow().Show();
        _toolbar.LangToggleRequested += () => LocalizationService.Instance.Switch();
        _toolbar.PositionToggleRequested += () =>
        {
            var onRight = !SettingsService.Instance.ToolbarOnRight;
            SettingsService.Instance.SetToolbarOnRight(onRight);
            _toolbar.ApplyPosition(onRight);
        };

        _tray    = new TrayService(_overlay, _toolbar);
        _hotkeys = new HotkeyService(_overlay, _toolbar);

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
