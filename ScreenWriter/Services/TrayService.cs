using System.Drawing;
using System.Windows.Forms;
using ScreenWriter.Windows;

namespace ScreenWriter.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon    _icon;
    private readonly OverlayWindow _overlay;
    private readonly ToolbarWindow _toolbar;

    public TrayService(OverlayWindow overlay, ToolbarWindow toolbar)
    {
        _overlay = overlay;
        _toolbar = toolbar;

        _icon = new NotifyIcon
        {
            Icon             = CreateIcon(),
            Text             = "Screen Writer",
            Visible          = true,
            ContextMenuStrip = BuildMenu(),
        };

        _icon.DoubleClick += (_, _) => Dispatch(Toggle);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("تفعيل / إيقاف الرسم  (Ctrl+Alt+D)", null, (_, _) => Dispatch(Toggle));
        menu.Items.Add("إظهار / إخفاء شريط الأدوات",         null, (_, _) => Dispatch(ToggleToolbar));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("مسح الكل  (Ctrl+Alt+C)",             null, (_, _) => Dispatch(_overlay.ConfirmedClearAll));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("خروج",                               null, (_, _) => Dispatch(System.Windows.Application.Current.Shutdown));
        return menu;
    }

    private void Toggle()
    {
        _overlay.ToggleDrawingMode();
        _toolbar.SyncMode(_overlay.IsDrawingMode);
    }

    private void ToggleToolbar()
    {
        if (_toolbar.IsVisible) _toolbar.Hide();
        else _toolbar.Show();
    }

    private static void Dispatch(Action action)
        => System.Windows.Application.Current.Dispatcher.Invoke(action);

    private static Icon CreateIcon()
    {
        var exeDir  = AppContext.BaseDirectory;
        var icoPath = System.IO.Path.Combine(exeDir, "Assets", "icon.ico");
        if (System.IO.File.Exists(icoPath))
            return new Icon(icoPath, 32, 32);

        // Fallback: programmatic icon
        using var bmp = new Bitmap(32, 32);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        using var fill = new SolidBrush(Color.FromArgb(17, 138, 178));
        g.FillEllipse(fill, 3, 3, 26, 26);
        using var pen = new System.Drawing.Pen(Color.White, 2.5f);
        g.DrawLine(pen, 10, 22, 22, 10);
        g.DrawLine(pen, 10, 22,  8, 24);
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
