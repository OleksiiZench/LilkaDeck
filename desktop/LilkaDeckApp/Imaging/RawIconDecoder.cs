using System;

namespace LilkaDeckApp.Imaging;

/// <summary>
/// Turns the contents of a .raw icon file into the pixel layout that Avalonia bitmaps use.
/// </summary>
public static class RawIconDecoder
{
    private const int BgraBytesPerPixel = 4;
    private const byte Opaque = 255;

    public static byte[] ToBgra(byte[] raw)
    {
        if (!RawIconFormat.HasValidSize(raw))
        {
            throw new ArgumentException($"A raw icon must be {RawIconFormat.FileSizeBytes} bytes, got {raw.Length}.", nameof(raw));
        }

        var bgra = new byte[RawIconFormat.PixelCount * BgraBytesPerPixel];

        for (int pixel = 0; pixel < RawIconFormat.PixelCount; pixel++)
        {
            ushort value = RawIconFormat.ReadPixel(raw, pixel * RawIconFormat.BytesPerPixel);
            var (red, green, blue) = Rgb565.Unpack(value);

            int offset = pixel * BgraBytesPerPixel;
            bgra[offset] = blue;
            bgra[offset + 1] = green;
            bgra[offset + 2] = red;
            bgra[offset + 3] = Opaque;
        }
        return bgra;
    }
}
