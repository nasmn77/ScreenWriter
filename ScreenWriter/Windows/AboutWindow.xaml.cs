using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ScreenWriter.Services;

namespace ScreenWriter.Windows;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        ApplyFlowDirection();
        LocalizationService.Instance.LanguageChanged += ApplyFlowDirection;
        TxtVersion.Text = $"v {UpdateService.CurrentVersion.ToString(3)}";
        Loaded += (_, _) => LoadIcon();
    }

    private void ApplyFlowDirection()
        => FlowDirection = LocalizationService.Instance.FlowDirection;

    protected override void OnClosed(EventArgs e)
    {
        LocalizationService.Instance.LanguageChanged -= ApplyFlowDirection;
        base.OnClosed(e);
    }

    private void LoadIcon()
    {
        try
        {
            var sri = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/icon.ico"));
            if (sri != null)
            {
                var decoder = BitmapDecoder.Create(sri.Stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                AppIcon.Source = decoder.Frames.OrderByDescending(f => f.PixelWidth).First();
            }
        }
        catch { }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

    private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        if (UpdateService.IsPackagedApp)
        {
            UpdateService.Instance.OpenStoreListing();
            return;
        }

        BtnCheckUpdates.IsEnabled = false;
        TxtUpdateStatus.Text = LocalizationService.Instance.Get("Str_CheckingUpdates");

        var info = await UpdateService.Instance.CheckAsync(CancellationToken.None);

        BtnCheckUpdates.IsEnabled = true;

        if (info != null)
        {
            TxtUpdateStatus.Text = "";
            Close();
            UpdateDialog.Show(info);
        }
        else if (UpdateService.Instance.LastCheckFailed)
        {
            TxtUpdateStatus.Text = LocalizationService.Instance.Get("Str_UpdateFailed");
        }
        else
        {
            TxtUpdateStatus.Text = LocalizationService.Instance.Get("Str_UpToDate");
        }
    }
}
