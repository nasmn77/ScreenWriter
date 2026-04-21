using System.IO;
using ScreenWriter.Models;

namespace ScreenWriter.Services;

public sealed class SettingsService
{
    public static SettingsService Instance { get; } = new();

    private static readonly string SettingsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScreenWriter", "settings.txt");

    public string      LastColorHex   { get; private set; } = "#E63946";
    public double      LastPenSize    { get; private set; } = DrawingSettings.DefaultPenSize;
    public DrawingTool LastTool       { get; private set; } = DrawingTool.Pen;
    public bool        ToolbarOnRight { get; private set; } = false;

    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsFile)) return;
            foreach (var raw in File.ReadAllLines(SettingsFile))
            {
                var idx = raw.IndexOf('=');
                if (idx < 1) continue;
                var key = raw[..idx].Trim();
                var val = raw[(idx + 1)..].Trim();
                switch (key)
                {
                    case "Color":
                        LastColorHex = val;
                        break;
                    case "PenSize":
                        if (double.TryParse(val,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var sz))
                            LastPenSize = Math.Clamp(sz, DrawingSettings.MinPenSize, DrawingSettings.MaxPenSize);
                        break;
                    case "Tool":
                        if (Enum.TryParse<DrawingTool>(val, out var tool) && tool != DrawingTool.Eraser)
                            LastTool = tool;
                        break;
                    case "ToolbarOnRight":
                        if (bool.TryParse(val, out var right))
                            ToolbarOnRight = right;
                        break;
                }
            }
        }
        catch { }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
            File.WriteAllLines(SettingsFile, [
                $"Color={LastColorHex}",
                $"PenSize={LastPenSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                $"Tool={LastTool}",
                $"ToolbarOnRight={ToolbarOnRight}",
            ]);
        }
        catch { }
    }

    public void SetColor(string hex)       { LastColorHex  = hex; }
    public void SetPenSize(double size)    { LastPenSize   = size; }
    public void SetTool(DrawingTool tool)  { LastTool      = tool; }
    public void SetToolbarOnRight(bool r)  { ToolbarOnRight = r; Save(); }
}