using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using ScreenWriter.Windows;

namespace ScreenWriter.Services;

public sealed class HotkeyService : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    private const int  WM_HOTKEY    = 0x0312;
    private const uint MOD_CONTROL  = 0x0002;
    private const uint MOD_ALT      = 0x0001;
    private const int  ID_TOGGLE    = 1;
    private const int  ID_CLEAR     = 2;
    private const int  ID_UNDO      = 3;
    private const int  ID_REDO      = 4;

    private readonly OverlayWindow _overlay;
    private readonly ToolbarWindow _toolbar;
    private nint          _hwnd;
    private HwndSource?   _source;

    public HotkeyService(OverlayWindow overlay, ToolbarWindow toolbar)
    {
        _overlay = overlay;
        _toolbar = toolbar;
        overlay.SourceInitialized += (_, _) => Attach();
    }

    private void Attach()
    {
        _hwnd   = new WindowInteropHelper(_overlay).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(Hook);

        RegisterHotKey(_hwnd, ID_TOGGLE, MOD_CONTROL | MOD_ALT,
            (uint)KeyInterop.VirtualKeyFromKey(Key.D));
        RegisterHotKey(_hwnd, ID_CLEAR, MOD_CONTROL | MOD_ALT,
            (uint)KeyInterop.VirtualKeyFromKey(Key.C));
        RegisterHotKey(_hwnd, ID_UNDO, MOD_CONTROL,
            (uint)KeyInterop.VirtualKeyFromKey(Key.Z));
        RegisterHotKey(_hwnd, ID_REDO, MOD_CONTROL,
            (uint)KeyInterop.VirtualKeyFromKey(Key.Y));
    }

    private nint Hook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg != WM_HOTKEY) return nint.Zero;

        switch ((int)wParam)
        {
            case ID_TOGGLE:
                _overlay.ToggleDrawingMode();
                _toolbar.SyncMode(_overlay.IsDrawingMode);
                _toolbar.BringToTopmost();
                handled = true;
                break;
            case ID_CLEAR:
                _overlay.ConfirmedClearAll();
                handled = true;
                break;
            case ID_UNDO:
                if (_overlay.IsDrawingMode)
                {
                    _overlay.Undo();
                    handled = true;
                }
                break;
            case ID_REDO:
                if (_overlay.IsDrawingMode)
                {
                    _overlay.Redo();
                    handled = true;
                }
                break;
        }
        return nint.Zero;
    }

    public void Dispose()
    {
        if (_hwnd != nint.Zero)
        {
            UnregisterHotKey(_hwnd, ID_TOGGLE);
            UnregisterHotKey(_hwnd, ID_CLEAR);
            UnregisterHotKey(_hwnd, ID_UNDO);
            UnregisterHotKey(_hwnd, ID_REDO);
        }
        _source?.RemoveHook(Hook);
    }
}
