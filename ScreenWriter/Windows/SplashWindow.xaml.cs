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
