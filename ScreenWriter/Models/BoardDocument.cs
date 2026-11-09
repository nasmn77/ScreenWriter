namespace ScreenWriter.Models;

/// <summary>Commands in the board "File" menu and their shortcuts.</summary>
public enum BoardCommand { New, SaveImage, CopyImage, Save, SaveAs, Open, InsertImage, Paste }

// Contents of board.json inside a .swboard file.
public sealed class BoardDocument
{
    public int FormatVersion { get; set; }
    public string Board { get; set; } = "";
    public double Width { get; set; }
    public double Height { get; set; }
    public List<BoardPage> Pages { get; set; } = [];
}

public sealed class BoardPage
{
    public string? Strokes { get; set; }
    public List<BoardElement> Elements { get; set; } = [];
}

public sealed class BoardElement
{
    public string Type { get; set; } = "";   // rect, ellipse, line, arrow, text, image
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public string? Color { get; set; }
    public double Thickness { get; set; }
    public string? Text { get; set; }
    public double FontSize { get; set; }
    public bool Bold { get; set; }
    public bool Rtl { get; set; }
    public string? Image { get; set; }
}
