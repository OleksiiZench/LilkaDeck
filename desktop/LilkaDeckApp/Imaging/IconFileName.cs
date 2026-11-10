namespace LilkaDeckApp.Imaging;

/// <summary>
/// Builds the file name an icon gets on the device. Names stay plain ASCII, because the SD card
/// stores them and the command that announces a file has to carry them unchanged.
/// </summary>
public static class IconFileName
{
    public const string Extension = ".raw";
    private const string FallbackStem = "icon";
    private const int MaxStemLength = 40;

    /// <summary>Turns the name of a picture, without its extension, into a device-safe icon file name.</summary>
    public static string FromSource(string sourceName) =>
        AsciiStem.From(sourceName, MaxStemLength, FallbackStem) + Extension;
}
