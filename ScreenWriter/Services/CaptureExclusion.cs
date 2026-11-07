using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ScreenWriter.Services;

/// <summary>
/// Hides a window from screen capture (Zoom, Teams, OBS…) while it stays visible to the user.
/// Needs Windows 10 2004 (build 19041) or later; on older builds it does nothing.
/// </summary>
public static class CaptureExclusion
{
    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(nint hWnd, uint dwAffinity);

    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    public static bool IsSupported => Environment.OSVersion.Version.Build >= 19041;

    public static void Apply(nint hwnd)
    {
        if (!IsSupported || hwnd == nint.Zero) return;
        SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
    }

    /// <summary>Applies to the window that hosts the given element (a window, popup or tooltip).</summary>
    public static void Apply(Visual? visual)
    {
        if (visual is null) return;
        if (PresentationSource.FromVisual(visual) is HwndSource src)
            Apply(src.Handle);
    }
}
