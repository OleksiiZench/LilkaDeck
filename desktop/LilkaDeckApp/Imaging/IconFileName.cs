using System.Text;

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
    public static string FromSource(string sourceName)
    {
        var stem = new StringBuilder();
        foreach (char character in sourceName)
        {
            stem.Append(Transliteration.Of(character));
        }
        return Tidy(stem.ToString()) + Extension;
    }

    // Collapses runs of underscores, trims the ends and keeps the name short.
    private static string Tidy(string raw)
    {
        var collapsed = new StringBuilder();
        foreach (char character in raw)
        {
            bool repeatsUnderscore = character == '_' && collapsed.Length > 0 && collapsed[collapsed.Length - 1] == '_';
            if (!repeatsUnderscore) collapsed.Append(character);
        }

        string stem = collapsed.ToString().Trim('_', '-');
        if (stem.Length > MaxStemLength)
        {
            stem = stem.Substring(0, MaxStemLength).TrimEnd('_', '-');
        }
        return stem.Length == 0 ? FallbackStem : stem;
    }
}
