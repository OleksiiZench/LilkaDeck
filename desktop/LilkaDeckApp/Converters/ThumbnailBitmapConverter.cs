using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Media.Imaging;

namespace LilkaDeckApp.Converters;

/// <summary>Shows a PNG or JPEG file as a small thumbnail, decoded at thumbnail size to save memory.</summary>
public sealed class ThumbnailBitmapConverter : CachedBitmapConverter
{
    private const int ThumbnailWidth = 128;
    
    public static ThumbnailBitmapConverter Instance { get; } = new();
    
    protected override Bitmap? Load(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return Bitmap.DecodeToWidth(stream, ThumbnailWidth);
        }
        catch (Exception ex)
        {
            // One broken picture must not stop the rest of the gallery from showing.
            Trace.TraceWarning($"Cannot load thumbnail '{path}': {ex.Message}");
            return null;
        }
    }
}
