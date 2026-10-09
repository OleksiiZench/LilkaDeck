using Avalonia.Media.Imaging;
using LilkaDeckApp.Imaging;

namespace LilkaDeckApp.Converters;

/// <summary>Shows a .raw icon file on a deck button.</summary>
public sealed class IconBitmapConverter : CachedBitmapConverter
{
    public static IconBitmapConverter Instance { get; } = new();
    
    protected override Bitmap? Load(string path) => RawIconBitmapLoader.Load(path);
}
