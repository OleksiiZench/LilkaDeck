using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace LilkaDeckApp;

/// <summary>
/// Provides utility methods for converting standard image formats 
/// into raw binary formats compatible with microcontroller displays.
/// </summary>
public static class ImageConverter
{
    /// <summary>
    /// Converts an image to a 64x64 16-bit RGB565 raw binary file.
    /// </summary>
    /// <param name="inputPath">The absolute or relative path to the source image (e.g., PNG, JPG).</param>
    /// <param name="outputPath">The destination path for the generated .raw file.</param>
    public static void ConvertToRgb565Raw(string inputPath, string outputPath)
    {
        // Load the source image into an RGBA 32-bit color space
        using var image = Image.Load<Rgba32>(inputPath);

        // Apply image transformations to match the hardware display requirements
        image.Mutate(x => x
            // Replace transparent backgrounds (e.g., from PNGs) with solid black 
            // to match the default background of the TFT display
            .BackgroundColor(Color.Black)
            // Force resize to exactly 64x64 pixels using stretching 
            // to prevent cropping out any parts of the original image
            .Resize(new ResizeOptions
            {
                Size = new Size(64, 64),
                Mode = ResizeMode.Stretch
            })
        );

        // Initialize binary streams to write the raw output file
        using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        // Iterate through every pixel sequentially (row by row)
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Rgba32 pixel = image[x, y];

                int r = pixel.R;
                int g = pixel.G;
                int b = pixel.B;

                // Compress the 24-bit RGB values into a single 16-bit RGB565 integer
                // Red: 5 bits, Green: 6 bits, Blue: 5 bits
                int rgb565 = ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3);

                // Write the 16-bit value to the file. 
                // BinaryWriter automatically uses Little-endian byte order.
                bw.Write((ushort)rgb565);
            }
        }
    }
}
