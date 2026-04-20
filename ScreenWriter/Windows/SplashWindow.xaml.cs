using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ScreenWriter.Windows;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
        if (System.IO.File.Exists(iconPath))
        {
            using var stream = System.IO.File.OpenRead(iconPath);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            AppIcon.Source = decoder.Frames.OrderByDescending(f => f.PixelWidth).First();
        }

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
        fadeIn.Completed += (_, _) =>
        {
            var holdTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.2) };
            holdTimer.Tick += (_, _) =>
            {
                holdTimer.Stop();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.45));
                fadeOut.Completed += (_, _) => Close();
                RootBorder.BeginAnimation(OpacityProperty, fadeOut);
            };
            holdTimer.Start();
        };
        RootBorder.BeginAnimation(OpacityProperty, fadeIn);
    }
}
