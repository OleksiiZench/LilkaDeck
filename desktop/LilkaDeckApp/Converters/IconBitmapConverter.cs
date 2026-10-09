using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LilkaDeckApp.Imaging;

namespace LilkaDeckApp.Converters;

/// <summary>
/// Turns the path of a .raw icon into a bitmap for an Image. Every bitmap is kept while its file is unchanged
/// and disposed when the file changes or is no longer shown, so bitmaps neither pile up nor are decoded twice.
/// Used on the UI thread only.
/// </summary>
public sealed class IconBitmapConverter : IValueConverter
{
    public static IconBitmapConverter Instance { get; } = new();

    private readonly Dictionary<string, CachedBitmap> _cache = new();

    private readonly record struct CachedBitmap(Bitmap Bitmap, DateTime ModifiedUtc, long Length);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    value is string { Length: > 0 } path ? GetBitmap(path) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotSupportedException();

    /// <summary>Disposes the bitmaps of every file that is not in the list.</summary>
    public void Retain(IEnumerable<string> shownPaths)
    {
        var keep = shownPaths.ToHashSet();
        foreach (string path in _cache.Keys.Where(path => !keep.Contains(path)).ToList())
        {
            Discard(path);
        }
    }

    private Bitmap? GetBitmap(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists)
        {
            Discard(path);
            return null;
        }

        if (_cache.TryGetValue(path, out var cached)
            && cached.ModifiedUtc == file.LastWriteTimeUtc
            && cached.Length == file.Length)
        {
            return cached.Bitmap;
        }

        Discard(path);
        var bitmap = RawIconBitmapLoader.Load(path);
        if (bitmap != null) _cache[path] = new CachedBitmap(bitmap, file.LastWriteTimeUtc, file.Length);
        return bitmap;
    }

    // Disposal waits until the binding has handed the new bitmap to the Image, so a bitmap is never freed while drawn.
    private void Discard(string path)
    {
        if (_cache.Remove(path, out var cached))
        {
            Dispatcher.UIThread.Post(cached.Bitmap.Dispose, DispatcherPriority.Background);
        }
    }
}
