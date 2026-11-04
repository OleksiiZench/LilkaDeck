using System.Text;

namespace LilkaDeckApp.Imaging;

/// <summary>
/// Sanitizes file names for device compatibility (ASCII only, safe characters).
/// Handles both stem-only and full names with extensions.
/// </summary>
public static class FileNameSanitizer
{
    private const string FallbackStem = "file";
    private const int MaxStemLength = 60;

    /// <summary>Sanitizes a full file name (including extension).</summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return FallbackStem;
        }

        string extension = System.IO.Path.GetExtension(fileName);
        string stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
        string sanitizedStem = SanitizeStem(stem);
        return sanitizedStem + extension;
    }

    /// <summary>Sanitizes just the stem part of a file name.</summary>
    public static string SanitizeStem(string stem)
    {
        if (string.IsNullOrEmpty(stem))
        {
            return FallbackStem;
        }

        var result = new StringBuilder();
        foreach (char character in stem)
        {
            result.Append(Transliteration.Of(character));
        }
        return Tidy(result.ToString());
    }

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
