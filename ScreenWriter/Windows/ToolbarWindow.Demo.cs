#if DEBUG
// Helpers for Store-screenshot demo scenes. Debug builds only.
namespace ScreenWriter.Windows;

public partial class ToolbarWindow
{
    /// <summary>Keeps the toolbar slid down for the screenshot.</summary>
    internal void PinForDemo()
    {
        _autoHide = false;
        _hideDelayTimer.Stop();
        SlideTo(0);
    }

    internal void OpenBoardMenuForDemo() => BoardPopup.IsOpen = true;

    internal void OpenFileMenuForDemo() => FilePopup.IsOpen = true;
}
#endif
