namespace LilkaDeckApp.Imaging;

/// <summary>
/// Conversion between 8-bit-per-channel colors and the 16-bit RGB565 format used by the display.
/// </summary>
public static class Rgb565
{
    public static ushort Pack(byte red, byte green, byte blue) =>
        (ushort)(((red & 0xF8) << 8) | ((green & 0xFC) << 3) | (blue >> 3));

    // Replicating the top bits into the low ones maps full intensity (0x1F) back to 0xFF.
    public static (byte Red, byte Green, byte Blue) Unpack(ushort value)
    {
        int red = (value >> 11) & 0x1F;
        int green = (value >> 5) & 0x3F;
        int blue = value & 0x1F;

        return (
            (byte)((red << 3) | (red >> 2)),
            (byte)((green << 2) | (green >> 4)),
            (byte)((blue << 3) | (blue >> 2)));
    }
}
