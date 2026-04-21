using System.IO;
using System.Windows;
using WpfApp = System.Windows.Application;
using WpfFlowDirection = System.Windows.FlowDirection;

namespace ScreenWriter.Services;

public sealed class LocalizationService
{
    public static LocalizationService Instance { get; } = new();

    private const string Arabic  = "ar";
    private const string English = "en";

    private static readonly string PrefFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScreenWriter", "language.txt");

    private static readonly Uri ArUri = new(
        "pack://application:,,,/ScreenWriter;component/Resources/Strings.ar.xaml",
        UriKind.Absolute);

    private static readonly Uri EnUri = new(
        "pack://application:,,,/ScreenWriter;component/Resources/Strings.en.xaml",
        UriKind.Absolute);

    private string _lang = Arabic;

    public event Action? LanguageChanged;

    public string        CurrentLanguage => _lang;
    public bool          IsArabic        => _lang == Arabic;
    public WpfFlowDirection FlowDirection => IsArabic
                                            ? WpfFlowDirection.RightToLeft
                                            : WpfFlowDirection.LeftToRight;

    public void Load()
    {
        _lang = ReadPref();
        ApplyDictionary(_lang, notify: false);
    }

    public void Switch()
    {
        _lang = IsArabic ? English : Arabic;
        ApplyDictionary(_lang, notify: true);
        WritePref(_lang);
    }

    public string Get(string key)
    {
        if (WpfApp.Current.TryFindResource(key) is string s) return s;
        return key;
    }

    private static void ApplyDictionary(string lang, bool notify)
    {
        var merged = WpfApp.Current.Resources.MergedDictionaries;

        var existing = merged.FirstOrDefault(d => d.Source == ArUri || d.Source == EnUri);
        if (existing is not null)
            merged.Remove(existing);

        merged.Add(new ResourceDictionary { Source = lang == Arabic ? ArUri : EnUri });

        if (notify)
            Instance.LanguageChanged?.Invoke();
    }

    private static string ReadPref()
    {
        try
        {
            if (File.Exists(PrefFile))
            {
                var val = File.ReadAllText(PrefFile).Trim().ToLowerInvariant();
                if (val is Arabic or English) return val;
            }
        }
        catch { }
        return Arabic;
    }

    private static void WritePref(string lang)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefFile)!);
            File.WriteAllText(PrefFile, lang);
        }
        catch { }
    }
}
