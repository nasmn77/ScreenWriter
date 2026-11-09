using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using ScreenWriter.Models;
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
    private const int  ID_BOARD_TRANSPARENT = 5;
    private const int  ID_BOARD_WHITE       = 6;
    private const int  ID_BOARD_BLACK       = 7;
    private const int  ID_SAVE_IMAGE        = 8;
    private const int  ID_SAVE              = 9;
    private const int  ID_OPEN              = 10;
    private const int  ID_PASTE             = 11;
    private const int  ID_NEW               = 12;

    private readonly OverlayWindow _overlay;
    private readonly ToolbarWindow _toolbar;
    private readonly Action<BoardMode> _onBoardHotkey;

    /// <summary>A board "File" shortcut was pressed.</summary>
    public event Action<BoardCommand>? CommandPressed;
    private nint          _hwnd;
    private HwndSource?   _source;

    public HotkeyService(OverlayWindow overlay, ToolbarWindow toolbar, Action<BoardMode> onBoardHotkey)
    {
        _overlay = overlay;
        _toolbar = toolbar;
        _onBoardHotkey = onBoardHotkey;
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
        // Ctrl+Z / Ctrl+Y are registered only while drawing mode is on. A registered hotkey
        // is swallowed system-wide, so keeping them always would break undo in other apps.
        _overlay.DrawingModeChanged += OnStateChanged;
        _overlay.BoardModeChanged   += OnBoardModeChanged;
        UpdateConditionalHotkeys();
        RegisterHotKey(_hwnd, ID_BOARD_TRANSPARENT, MOD_CONTROL | MOD_ALT,
            (uint)KeyInterop.VirtualKeyFromKey(Key.T));
        RegisterHotKey(_hwnd, ID_BOARD_WHITE, MOD_CONTROL | MOD_ALT,
            (uint)KeyInterop.VirtualKeyFromKey(Key.W));
        RegisterHotKey(_hwnd, ID_BOARD_BLACK, MOD_CONTROL | MOD_ALT,
            (uint)KeyInterop.VirtualKeyFromKey(Key.B));
    }

    private bool _undoHotkeysOn;
    private bool _boardHotkeysOn;
    private int  _suspendCount;

    /// <summary>Releases Ctrl+Z, Ctrl+V and friends while a dialog needs them. Calls nest.</summary>
    public void Suspend() { _suspendCount++; UpdateConditionalHotkeys(); }

    public void Resume()  { _suspendCount--; UpdateConditionalHotkeys(); }

    private void OnStateChanged(bool _)          => UpdateConditionalHotkeys();
    private void OnBoardModeChanged(BoardMode _) => UpdateConditionalHotkeys();

    private void UpdateConditionalHotkeys()
    {
        if (_hwnd == nint.Zero) return;
        SetUndoHotkeys(_suspendCount == 0 && _overlay.IsDrawingMode);
        SetBoardHotkeys(_suspendCount == 0 && _overlay.IsBoard);
    }

    private void SetUndoHotkeys(bool on)
    {
        if (on == _undoHotkeysOn) return;
        _undoHotkeysOn = on;
        if (on)
        {
            RegisterHotKey(_hwnd, ID_UNDO, MOD_CONTROL,
                (uint)KeyInterop.VirtualKeyFromKey(Key.Z));
            RegisterHotKey(_hwnd, ID_REDO, MOD_CONTROL,
                (uint)KeyInterop.VirtualKeyFromKey(Key.Y));
        }
        else
        {
            UnregisterHotKey(_hwnd, ID_UNDO);
            UnregisterHotKey(_hwnd, ID_REDO);
        }
    }

    // A registered hotkey is swallowed system-wide, so these exist only while a board is open.
    private void SetBoardHotkeys(bool on)
    {
        if (on == _boardHotkeysOn) return;
        _boardHotkeysOn = on;
        if (on)
        {
            RegisterHotKey(_hwnd, ID_SAVE_IMAGE, MOD_CONTROL | MOD_ALT,
                (uint)KeyInterop.VirtualKeyFromKey(Key.S));
            RegisterHotKey(_hwnd, ID_SAVE, MOD_CONTROL,
                (uint)KeyInterop.VirtualKeyFromKey(Key.S));
            RegisterHotKey(_hwnd, ID_OPEN, MOD_CONTROL,
                (uint)KeyInterop.VirtualKeyFromKey(Key.O));
            RegisterHotKey(_hwnd, ID_PASTE, MOD_CONTROL,
                (uint)KeyInterop.VirtualKeyFromKey(Key.V));
            RegisterHotKey(_hwnd, ID_NEW, MOD_CONTROL,
                (uint)KeyInterop.VirtualKeyFromKey(Key.N));
        }
        else
        {
            UnregisterHotKey(_hwnd, ID_SAVE_IMAGE);
            UnregisterHotKey(_hwnd, ID_SAVE);
            UnregisterHotKey(_hwnd, ID_OPEN);
            UnregisterHotKey(_hwnd, ID_PASTE);
            UnregisterHotKey(_hwnd, ID_NEW);
        }
    }

    private nint Hook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg != WM_HOTKEY || _suspendCount > 0) return nint.Zero;

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
                if (_overlay.IsEditingText) _overlay.TextBoxUndoRedo(redo: false);
                else                        _overlay.Undo();
                handled = true;
                break;
            case ID_REDO:
                if (_overlay.IsEditingText) _overlay.TextBoxUndoRedo(redo: true);
                else                        _overlay.Redo();
                handled = true;
                break;
            case ID_BOARD_TRANSPARENT:
                _onBoardHotkey(BoardMode.Transparent);
                handled = true;
                break;
            case ID_BOARD_WHITE:
                _onBoardHotkey(BoardMode.White);
                handled = true;
                break;
            case ID_BOARD_BLACK:
                _onBoardHotkey(BoardMode.Black);
                handled = true;
                break;
            case ID_SAVE_IMAGE:
                CommandPressed?.Invoke(BoardCommand.SaveImage);
                handled = true;
                break;
            case ID_SAVE:
                CommandPressed?.Invoke(BoardCommand.Save);
                handled = true;
                break;
            case ID_OPEN:
                CommandPressed?.Invoke(BoardCommand.Open);
                handled = true;
                break;
            case ID_NEW:
                CommandPressed?.Invoke(BoardCommand.New);
                handled = true;
                break;
            case ID_PASTE:
                if (_overlay.IsEditingText) _overlay.TextBoxPaste();
                else                        CommandPressed?.Invoke(BoardCommand.Paste);
                handled = true;
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
            SetUndoHotkeys(false);
            SetBoardHotkeys(false);
            UnregisterHotKey(_hwnd, ID_BOARD_TRANSPARENT);
            UnregisterHotKey(_hwnd, ID_BOARD_WHITE);
            UnregisterHotKey(_hwnd, ID_BOARD_BLACK);
        }
        _overlay.DrawingModeChanged -= OnStateChanged;
        _overlay.BoardModeChanged   -= OnBoardModeChanged;
        _source?.RemoveHook(Hook);
    }
}
