using System.IO;

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

        string extension = Path.GetExtension(fileName);
        string stem = Path.GetFileNameWithoutExtension(fileName);
        return SanitizeStem(stem) + extension;
    }

    /// <summary>Sanitizes just the stem part of a file name.</summary>
    public static string SanitizeStem(string stem) =>
        AsciiStem.From(stem, MaxStemLength, FallbackStem);
}
