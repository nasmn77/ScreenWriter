using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenWriter.Services;

/// <summary>An image placed on a board: the bytes kept in the file and the bitmap on screen.</summary>
public sealed record BoardImage(byte[] Bytes, string Extension, BitmapSource Bitmap)
{
    public const long MaxInputBytes = 50 * 1024 * 1024;
    private const int MaxLongSide = 3840;
    private const long MaxInputPixels = 200_000_000;

    public static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    public static bool IsSupportedFile(string path)
        => SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

    public static BoardImage FromFile(string path)
    {
        if (new FileInfo(path).Length > MaxInputBytes)
            throw new InvalidDataException("Image file is too large.");
        return FromBytes(File.ReadAllBytes(path));
    }

    /// <summary>Prepares encoded image data for a board: applies EXIF rotation and limits the size.</summary>
    public static BoardImage FromBytes(byte[] bytes)
    {
        var (frame, decoder) = OpenFrame(bytes);
        if ((long)frame.PixelWidth * frame.PixelHeight > MaxInputPixels)
            throw new InvalidDataException("Image is too large.");

        bool isPng  = decoder is PngBitmapDecoder;
        bool isJpeg = decoder is JpegBitmapDecoder;
        if (!isPng && !isJpeg && decoder is not BmpBitmapDecoder && decoder is not GifBitmapDecoder)
            throw new NotSupportedException("Unsupported image format.");

        int orientation = ReadOrientation(frame);
        bool mustScale  = Math.Max(frame.PixelWidth, frame.PixelHeight) > MaxLongSide;

        // Original bytes are kept when nothing about the picture has to change.
        if ((isPng || isJpeg) && !mustScale && orientation == 1)
            return new BoardImage(bytes, isPng ? "png" : "jpg", Load(bytes));

        var bitmap = ApplyOrientation(Load(bytes, mustScale, frame.PixelWidth >= frame.PixelHeight), orientation);
        return isJpeg
            ? new BoardImage(Encode(bitmap, new JpegBitmapEncoder { QualityLevel = 90 }), "jpg", bitmap)
            : new BoardImage(Encode(bitmap, new PngBitmapEncoder()), "png", bitmap);
    }

    /// <summary>Wraps a bitmap that has no encoded form, such as one from the clipboard.</summary>
    public static BoardImage FromBitmap(BitmapSource source)
    {
        var bitmap = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);
        bitmap.Freeze();
        return FromBytes(Encode(bitmap, new PngBitmapEncoder()));
    }

    /// <summary>Decodes an image stored in a .swboard file, rejecting oversized pictures.</summary>
    public static BitmapSource DecodeStored(byte[] bytes, int maxSide)
    {
        var (frame, decoder) = OpenFrame(bytes);
        if (decoder is not PngBitmapDecoder and not JpegBitmapDecoder)
            throw new InvalidDataException("Unexpected image format.");
        if (frame.PixelWidth > maxSide || frame.PixelHeight > maxSide)
            throw new InvalidDataException("Image is too large.");
        return Load(bytes);
    }

    public static byte[] Encode(BitmapSource bitmap, BitmapEncoder encoder)
    {
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    // Reads the header only, so the size can be checked before decoding the pixels.
    private static (BitmapFrame Frame, BitmapDecoder Decoder) OpenFrame(byte[] bytes)
    {
        var decoder = BitmapDecoder.Create(new MemoryStream(bytes),
            BitmapCreateOptions.DelayCreation | BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.None);
        if (decoder.Frames.Count == 0) throw new InvalidDataException("Image has no frames.");
        return (decoder.Frames[0], decoder);
    }

    private static BitmapSource Load(byte[] bytes, bool scale = false, bool landscape = true)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption  = BitmapCacheOption.OnLoad;
        image.StreamSource = new MemoryStream(bytes);
        if (scale)
        {
            if (landscape) image.DecodePixelWidth  = MaxLongSide;
            else           image.DecodePixelHeight = MaxLongSide;
        }
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static int ReadOrientation(BitmapFrame frame)
    {
        try
        {
            if (frame.Metadata is BitmapMetadata meta
                && meta.GetQuery("System.Photo.Orientation") is ushort value
                && value is >= 1 and <= 8)
                return value;
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or ArgumentException) { }
        return 1;
    }

    private static BitmapSource ApplyOrientation(BitmapSource source, int orientation)
    {
        if (orientation == 1) return source;

        var group = new TransformGroup();
        if (orientation is 2 or 5 or 7) group.Children.Add(new ScaleTransform(-1, 1));
        group.Children.Add(new RotateTransform(orientation switch
        {
            3 or 4 => 180,
            5 or 8 => 270,
            6 or 7 => 90,
            _      => 0,
        }));

        var result = new TransformedBitmap(source, group);
        result.Freeze();
        return result;
    }
}
