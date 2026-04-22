using System.Windows;
using System.Windows.Input;
using ScreenWriter.Services;

namespace ScreenWriter.Windows;

public partial class UpdateDialog : Window
{
    private readonly UpdateInfo _info;
    private CancellationTokenSource? _cts;

    public UpdateDialog(UpdateInfo info)
    {
        InitializeComponent();
        _info = info;
        ApplyFlowDirection();
        LocalizationService.Instance.LanguageChanged += ApplyFlowDirection;

        TxtCurrent.Text = UpdateService.CurrentVersion.ToString(3);
        TxtLatest.Text  = info.Version.ToString(3);
        TxtNotes.Text   = string.IsNullOrWhiteSpace(info.ReleaseNotes)
                            ? ""
                            : info.ReleaseNotes.Trim();
    }

    public static void Show(UpdateInfo info)
    {
        var dlg = new UpdateDialog(info);
        dlg.Show();
    }

    private void ApplyFlowDirection()
        => FlowDirection = LocalizationService.Instance.FlowDirection;

    protected override void OnClosed(EventArgs e)
    {
        LocalizationService.Instance.LanguageChanged -= ApplyFlowDirection;
        _cts?.Cancel();
        _cts?.Dispose();
        base.OnClosed(e);
    }

    private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        PanelButtons.Visibility  = Visibility.Collapsed;
        PanelProgress.Visibility = Visibility.Visible;

        _cts = new CancellationTokenSource();
        var progress = new Progress<double>(p =>
        {
            Progress.Value    = p * 100;
            TxtProgress.Text  = $"{(int)(p * 100)}%  ({FormatBytes((long)(_info.SizeBytes * p))} / {FormatBytes(_info.SizeBytes)})";
        });

        try
        {
            var path = await UpdateService.Instance.DownloadAsync(_info, progress, _cts.Token);
            UpdateService.Instance.LaunchAndExit(path);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            TxtProgress.Text = ex.Message;
            PanelButtons.Visibility  = Visibility.Visible;
            PanelProgress.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        Close();
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 KB";
        if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
        return $"{bytes / 1024.0 / 1024.0:F1} MB";
    }
}
