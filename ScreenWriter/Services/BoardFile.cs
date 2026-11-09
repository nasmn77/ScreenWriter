using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Ink;
using ScreenWriter.Models;

namespace ScreenWriter.Services;

/// <summary>A .swboard file that could not be opened. Key is the message to show the user.</summary>
public sealed class BoardFileException(string key) : Exception(key)
{
    public string Key { get; } = key;
}

/// <summary>A board read from disk and fully validated, ready to be shown.</summary>
public sealed record LoadedBoard(
    BoardDocument Document,
    BoardPage Page,
    BoardMode Mode,
    StrokeCollection Strokes,
    Dictionary<string, BoardImage> Images);

/// <summary>
/// Reads and writes .swboard files: a ZIP holding board.json, the ink and the images.
/// These files can come from other people, so everything is validated in memory first.
/// </summary>
public static partial class BoardFile
{
    public const string Extension = ".swboard";
    public const int CurrentVersion = 1;

    private const string JsonEntry    = "board.json";
    private const string StrokesEntry = "pages/0/strokes.isf";

    private const long MaxFileBytes    = 200L * 1024 * 1024;
    private const long MaxTotalBytes   = 500L * 1024 * 1024;
    private const long MaxJsonBytes    = 32L * 1024 * 1024;
    private const long MaxStrokesBytes = 100L * 1024 * 1024;
    private const long MaxImageBytes   = 50L * 1024 * 1024;
    private const int  MaxEntries      = 256;
    private const int  MaxElements     = 20_000;
    private const int  MaxImages       = 200;
    private const int  MaxStrokes      = 200_000;
    private const int  MaxImageSide    = 8192;
    private const int  MaxTextLength   = 100_000;
    private const double MaxCoordinate = 200_000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [GeneratedRegex(@"^images/[0-9a-f]{32}\.(png|jpg)$")]
    private static partial Regex ImageNameRegex();

    [GeneratedRegex(@"^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$")]
    private static partial Regex ColorRegex();

    public static bool IsBoardFile(string path)
        => string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase);

    public static string NewImageName(string extension) => $"images/{Guid.NewGuid():N}.{extension}";

    // ── Save ──────────────────────────────────────────────────────────────────
    /// <summary>Writes to a temporary file first, so a failure cannot damage an existing board.</summary>
    public static void Save(string path, BoardDocument document, StrokeCollection strokes,
        IReadOnlyDictionary<string, byte[]> images)
    {
        var page = document.Pages[0];
        page.Strokes = strokes.Count > 0 ? StrokesEntry : null;

        var temp = path + ".tmp";
        try
        {
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write))
            using (var zip  = new ZipArchive(file, ZipArchiveMode.Create))
            {
                WriteEntry(zip, JsonEntry, CompressionLevel.Optimal,
                    JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions));

                if (page.Strokes is not null)
                {
                    using var ms = new MemoryStream();
                    strokes.Save(ms, true);
                    WriteEntry(zip, StrokesEntry, CompressionLevel.Optimal, ms.ToArray());
                }

                // Images are already compressed
                foreach (var (name, bytes) in images)
                    WriteEntry(zip, name, CompressionLevel.NoCompression, bytes);
            }
            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            try { File.Delete(temp); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            throw;
        }
    }

    private static void WriteEntry(ZipArchive zip, string name, CompressionLevel level, byte[] data)
    {
        using var stream = zip.CreateEntry(name, level).Open();
        stream.Write(data);
    }

    // ── Load ──────────────────────────────────────────────────────────────────
    public static LoadedBoard Load(string path)
    {
        try
        {
            if (new FileInfo(path).Length > MaxFileBytes) throw Invalid();

            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var zip  = new ZipArchive(file, ZipArchiveMode.Read);
            if (zip.Entries.Count > MaxEntries) throw Invalid();

            long budget = MaxTotalBytes;
            var json = ReadEntry(zip, JsonEntry, MaxJsonBytes, ref budget);
            var document = JsonSerializer.Deserialize<BoardDocument>(json, JsonOptions) ?? throw Invalid();

            if (document.FormatVersion > CurrentVersion) throw new BoardFileException("Str_FileNewer");
            if (document.FormatVersion < 1) throw Invalid();

            var mode = document.Board switch
            {
                "White" => BoardMode.White,
                "Black" => BoardMode.Black,
                _       => throw Invalid(),
            };
            if (!IsPositive(document.Width) || !IsPositive(document.Height) || document.Pages.Count == 0)
                throw Invalid();

            var page = document.Pages[0];
            if (page.Elements.Count > MaxElements) throw Invalid();
            foreach (var element in page.Elements) Validate(element);

            var strokes = new StrokeCollection();
            if (page.Strokes is not null)
            {
                if (page.Strokes != StrokesEntry) throw Invalid();
                using var ms = new MemoryStream(ReadEntry(zip, StrokesEntry, MaxStrokesBytes, ref budget));
                strokes = new StrokeCollection(ms);
                if (strokes.Count > MaxStrokes) throw Invalid();
            }

            var images = new Dictionary<string, BoardImage>();
            foreach (var element in page.Elements)
            {
                if (element.Type != "image" || images.ContainsKey(element.Image!)) continue;
                if (images.Count >= MaxImages) throw Invalid();

                var bytes  = ReadEntry(zip, element.Image!, MaxImageBytes, ref budget);
                var bitmap = BoardImage.DecodeStored(bytes, MaxImageSide);
                var ext    = Path.GetExtension(element.Image!).TrimStart('.');
                images[element.Image!] = new BoardImage(bytes, ext, bitmap);
            }

            return new LoadedBoard(document, page, mode, strokes, images);
        }
        catch (BoardFileException) { throw; }
        catch (Exception) { throw Invalid(); }   // anything malformed is reported the same way
    }

    private static BoardFileException Invalid() => new("Str_FileInvalid");

    // The size in the ZIP header can lie, so the bytes actually read are counted.
    private static byte[] ReadEntry(ZipArchive zip, string name, long maxBytes, ref long budget)
    {
        var entry = zip.GetEntry(name) ?? throw Invalid();
        using var input  = entry.Open();
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > maxBytes || total > budget) throw Invalid();
            output.Write(buffer, 0, read);
        }
        budget -= total;
        return output.ToArray();
    }

    private static void Validate(BoardElement e)
    {
        double[] numbers = [e.X, e.Y, e.W, e.H, e.X1, e.Y1, e.X2, e.Y2];
        if (numbers.Any(n => !double.IsFinite(n) || Math.Abs(n) > MaxCoordinate)) throw Invalid();

        switch (e.Type)
        {
            case "rect" or "ellipse":
                if (!IsPositive(e.W) || !IsPositive(e.H)) throw Invalid();
                RequireStroke(e);
                break;
            case "line" or "arrow":
                RequireStroke(e);
                break;
            case "text":
                RequireColor(e);
                if (e.Text is null || e.Text.Length > MaxTextLength) throw Invalid();
                if (!double.IsFinite(e.FontSize) || e.FontSize is < 1 or > 2000) throw Invalid();
                break;
            case "image":
                if (!IsPositive(e.W) || !IsPositive(e.H)) throw Invalid();
                if (e.Image is null || !ImageNameRegex().IsMatch(e.Image)) throw Invalid();
                break;
            default:
                throw Invalid();
        }
    }

    private static void RequireStroke(BoardElement e)
    {
        RequireColor(e);
        if (!double.IsFinite(e.Thickness) || e.Thickness is < 0 or > 500) throw Invalid();
    }

    private static void RequireColor(BoardElement e)
    {
        if (e.Color is null || !ColorRegex().IsMatch(e.Color)) throw Invalid();
    }

    private static bool IsPositive(double value)
        => double.IsFinite(value) && value > 0 && value <= MaxCoordinate;
}
