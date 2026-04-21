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
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
        if (System.IO.File.Exists(iconPath))
        {
            using var stream = System.IO.File.OpenRead(iconPath);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            AppIcon.Source = decoder.Frames.OrderByDescending(f => f.PixelWidth).First();
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
}
