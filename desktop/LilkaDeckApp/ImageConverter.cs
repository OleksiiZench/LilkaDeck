using Avalonia.Media.Imaging;
using LilkaDeckApp.Imaging;

namespace LilkaDeckApp;

/// <summary>
/// Compatibility facade over the Imaging namespace. It exists only until MainWindow
/// stops calling it directly, and then it can be deleted.
/// </summary>
public static class ImageConverter
{
    public static void ConvertToRgb565Raw(string inputPath, string outputPath) =>
        RawIconEncoder.ConvertFile(inputPath, outputPath);

    public static Bitmap? DecodeRgb565RawToBitmap(string rawPath) =>
        RawIconBitmapLoader.Load(rawPath);
}
