using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System;

namespace LilkaDeckApp;

public static class ImageConverter
{
    public static void ConvertToRgb565Raw(string inputPath, string outputPath)
    {
        // Load the source image
        using var image = Image.Load<Rgba32>(inputPath);

        // Resize the image to 64x64 pixels
        image.Mutate(x => x.Resize(64, 64));

        // Open a file stream for writing
        using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        // Iterate through each pixel
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Rgba32 pixel = image[x, y];

                int r = pixel.R;
                int g = pixel.G;
                int b = pixel.B;

                // Convert 24-bit RGB to 16-bit RGB565
                int rgb565 = ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3);

                // BinaryWriter automatically writes ushort (2 bytes) in Little-endian format
                bw.Write((ushort)rgb565);
            }
        }
    }
}
