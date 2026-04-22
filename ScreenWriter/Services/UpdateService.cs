using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace ScreenWriter.Services;

public sealed class UpdateService
{
    public static UpdateService Instance { get; } = new();

    // TODO: عند نشر التطبيق في Microsoft Store، حدّث هذه القيمة بالـ Product ID الحقيقي
    // (يظهر في apps.microsoft.com/detail/XXXXXXXXXXXX)
    private const string StoreProductId = "9NBLGGH4XXXXXX";

    private const string LatestReleaseUrl =
        "https://api.github.com/repos/nasmn77/ScreenWriter/releases/latest";

    private const string AssetName = "Setup_ScreenWriter.exe";

    private static readonly HttpClient _http = CreateHttpClient();

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

    public bool LastCheckFailed { get; private set; }

    public async Task<UpdateInfo?> CheckAsync(CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var json = await _http.GetStringAsync(LatestReleaseUrl, cts.Token);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var latest = ParseVersion(tag);
            LastCheckFailed = false;
            if (latest is null || latest <= CurrentVersion) return null;

            string url = "";
            long size = 0;
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                if (string.Equals(asset.GetProperty("name").GetString(),
                                  AssetName, StringComparison.OrdinalIgnoreCase))
                {
                    url  = asset.GetProperty("browser_download_url").GetString() ?? "";
                    size = asset.GetProperty("size").GetInt64();
                    break;
                }
            }
            if (string.IsNullOrEmpty(url)) return null;

            var notes = root.TryGetProperty("body", out var b) ? (b.GetString() ?? "") : "";
            return new UpdateInfo(latest, url, size, notes);
        }
        catch
        {
            LastCheckFailed = true;
            return null;
        }
    }

    public async Task<string> DownloadAsync(
        UpdateInfo info,
        IProgress<double> progress,
        CancellationToken ct)
    {
        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"ScreenWriter_Update_{info.Version}.exe");

        using var resp = await _http.GetAsync(
            info.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            ct);
        resp.EnsureSuccessStatusCode();

        var total = resp.Content.Headers.ContentLength ?? info.SizeBytes;
        await using var src = await resp.Content.ReadAsStreamAsync(ct);
        await using var dst = File.Create(tempPath);

        var buffer = new byte[81920];
        long read = 0;
        int n;
        while ((n = await src.ReadAsync(buffer, ct)) > 0)
        {
            await dst.WriteAsync(buffer.AsMemory(0, n), ct);
            read += n;
            if (total > 0)
                progress.Report((double)read / total);
        }
        return tempPath;
    }

    public void LaunchAndExit(string installerPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
        }
        catch { return; }
        System.Windows.Application.Current.Shutdown();
    }

    private static Version? ParseVersion(string tag)
    {
        var t = tag.TrimStart('v', 'V').Trim();
        return Version.TryParse(t, out var v) ? v : null;
    }

    private static HttpClient CreateHttpClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"ScreenWriter/{CurrentVersion}");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return c;
    }
}

public sealed record UpdateInfo(
    Version Version,
    string  DownloadUrl,
    long    SizeBytes,
    string  ReleaseNotes);
