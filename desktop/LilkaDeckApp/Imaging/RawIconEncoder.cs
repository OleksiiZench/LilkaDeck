using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LilkaDeckApp.Imaging;

/// <summary>
/// Converts ordinary images (PNG, JPG, ...) into the raw icon format the firmware displays.
/// </summary>
public static class RawIconEncoder
{
    public static byte[] Encode(string imagePath)
    {
        using var image = Image.Load<Rgba32>(imagePath);

        image.Mutate(x => x
            // Transparent areas become black to match the display background.
            .BackgroundColor(Color.Black)
            // Stretching (instead of cropping or padding) keeps every part of the original visible.
            .Resize(new ResizeOptions
            {
                Size = new Size(RawIconFormat.IconSize, RawIconFormat.IconSize),
                Mode = ResizeMode.Stretch
            }));

        return PackPixels(image);
    }

    private static byte[] PackPixels(Image<Rgba32> image)
    {
        var buffer = new byte[image.Width * image.Height * RawIconFormat.BytesPerPixel];
        int offset = 0;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Rgba32 pixel = image[x, y];
                RawIconFormat.WritePixel(buffer, offset, Rgb565.Pack(pixel.R, pixel.G, pixel.B));
                offset += RawIconFormat.BytesPerPixel;
            }
        }
        return buffer;
    }
}
