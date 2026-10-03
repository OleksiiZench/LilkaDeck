using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace LilkaDeckApp.Imaging;

/// <summary>
/// Loads a .raw icon file as an Avalonia bitmap for display in the UI.
/// </summary>
public static class RawIconBitmapLoader
{
    private const double Dpi = 96;

    /// <summary>
    /// Returns null when the file is missing, has the wrong size or cannot be decoded.
    /// </summary>
    public static Bitmap? Load(string rawPath)
    {
        if (!File.Exists(rawPath)) return null;

        try
        {
            byte[] raw = File.ReadAllBytes(rawPath);
            return RawIconFormat.HasValidSize(raw) ? CreateBitmap(RawIconDecoder.ToBgra(raw)) : null;
        }
        catch (Exception ex)
        {
            // A broken icon must not stop the other buttons from being drawn.
            Trace.TraceWarning($"Cannot load icon '{rawPath}': {ex.Message}");
            return null;
        }
    }

    private static Bitmap CreateBitmap(byte[] bgraPixels)
    {
        var bitmap = new WriteableBitmap(
            new Avalonia.PixelSize(RawIconFormat.IconSize, RawIconFormat.IconSize),
            new Avalonia.Vector(Dpi, Dpi),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using (var framebuffer = bitmap.Lock())
        {
            Marshal.Copy(bgraPixels, 0, framebuffer.Address, bgraPixels.Length);
        }
        return bitmap;
    }
}
