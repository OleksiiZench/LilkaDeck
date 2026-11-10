using System.Text;

namespace LilkaDeckApp.Imaging;

/// <summary>
/// Turns arbitrary text into a plain ASCII stem that is safe to store on the device's SD card.
/// </summary>
internal static class AsciiStem
{
    public static string From(string text, int maxLength, string fallback)
    {
        var transliterated = new StringBuilder();
        foreach (char character in text)
        {
            transliterated.Append(Transliteration.Of(character));
        }
        return Tidy(transliterated.ToString(), maxLength, fallback);
    }

    // Collapses runs of underscores, trims the ends and keeps the name short.
    private static string Tidy(string raw, int maxLength, string fallback)
    {
        var collapsed = new StringBuilder();
        foreach (char character in raw)
        {
            bool repeatsUnderscore = character == '_' && collapsed.Length > 0 && collapsed[collapsed.Length - 1] == '_';
            if (!repeatsUnderscore) collapsed.Append(character);
        }

        string stem = collapsed.ToString().Trim('_', '-');
        if (stem.Length > maxLength)
        {
            stem = stem.Substring(0, maxLength).TrimEnd('_', '-');
        }
        return stem.Length == 0 ? fallback : stem;
    }
}
