using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenWriter.Services;

public sealed class UpdateService
{
    public static UpdateService Instance { get; } = new();

    private const string StoreProductId = "9NF25DZQXLCT";

    private static bool? _isPackaged;

    public static bool IsPackagedApp
    {
        get
        {
            if (_isPackaged.HasValue) return _isPackaged.Value;
            _isPackaged = DetectPackaged();
            return _isPackaged.Value;
        }
    }

    // APPMODEL_ERROR_NO_PACKAGE = 15700
    // ERROR_INSUFFICIENT_BUFFER = 122 → يعني الحزمة موجودة وسلسلتنا صغيرة
    private const int APPMODEL_ERROR_NO_PACKAGE  = 15700;
    private const int ERROR_INSUFFICIENT_BUFFER  = 122;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int GetCurrentPackageFullName(
        ref int packageFullNameLength,
        StringBuilder? packageFullName);

    private static bool DetectPackaged()
    {
        try
        {
            int len = 0;
            int rc  = GetCurrentPackageFullName(ref len, null);
            return rc != APPMODEL_ERROR_NO_PACKAGE;
        }
        catch
        {
            return false;
        }
    }

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public void OpenStoreListing()
    {
        try
        {
            Process.Start(new ProcessStartInfo(
                $"ms-windows-store://pdp/?productid={StoreProductId}")
            { UseShellExecute = true });
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://apps.microsoft.com/search?query=Screen+Writer")
                { UseShellExecute = true });
            }
            catch { }
        }
    }

    public void OpenStoreReview()
    {
        try
        {
            Process.Start(new ProcessStartInfo(
                $"ms-windows-store://review/?ProductId={StoreProductId}")
            { UseShellExecute = true });
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo($"https://apps.microsoft.com/detail/{StoreProductId}")
                { UseShellExecute = true });
            }
            catch { }
        }
    }

    /// <summary>
    /// Asks the Microsoft Store whether a newer version of this app exists.
    /// Works only in the Store-installed package; any failure counts as "no update".
    /// </summary>
    public static async Task<bool> IsStoreUpdateAvailableAsync()
    {
        if (!IsPackagedApp) return false;
        try
        {
            var context = global::Windows.Services.Store.StoreContext.GetDefault();
            var updates = await context.GetAppAndOptionalStorePackageUpdatesAsync();
            return updates.Count > 0;
        }
        catch (Exception)
        {
            return false;   // offline, Store unavailable, or not a Store install
        }
    }
}
