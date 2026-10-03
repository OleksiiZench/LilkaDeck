using System;

namespace LilkaDeckApp.Imaging;

/// <summary>
/// Layout of the icon files the firmware reads from the SD card:
/// 64x64 pixels, RGB565, little-endian, row by row (8192 bytes in total).
/// </summary>
public static class RawIconFormat
{
    public const int IconSize = 64;
    public const int BytesPerPixel = 2;
    public const int PixelCount = IconSize * IconSize;
    public const int FileSizeBytes = PixelCount * BytesPerPixel;

    public static bool HasValidSize(byte[] raw) => raw.Length == FileSizeBytes;

    public static void WritePixel(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)(value >> 8);
    }

    public static ushort ReadPixel(ReadOnlySpan<byte> buffer, int offset) =>
        (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
}
