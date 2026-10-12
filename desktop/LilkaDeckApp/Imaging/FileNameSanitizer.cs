using System.Collections.Generic;
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

    // Ukrainian transliteration, plus the Russian letters that do not exist in Ukrainian.
    private static readonly Dictionary<char, string> Latin = new()
    {
        ['а'] = "a",
        ['б'] = "b",
        ['в'] = "v",
        ['г'] = "h",
        ['ґ'] = "g",
        ['д'] = "d",
        ['е'] = "e",
        ['є'] = "ye",
        ['ж'] = "zh",
        ['з'] = "z",
        ['и'] = "y",
        ['і'] = "i",
        ['ї'] = "yi",
        ['й'] = "y",
        ['к'] = "k",
        ['л'] = "l",
        ['м'] = "m",
        ['н'] = "n",
        ['о'] = "o",
        ['п'] = "p",
        ['р'] = "r",
        ['с'] = "s",
        ['т'] = "t",
        ['у'] = "u",
        ['ф'] = "f",
        ['х'] = "kh",
        ['ц'] = "ts",
        ['ч'] = "ch",
        ['ш'] = "sh",
        ['щ'] = "shch",
        ['ь'] = "",
        ['ю'] = "yu",
        ['я'] = "ya",
        ['ы'] = "y",
        ['э'] = "e",
        ['ъ'] = "",
        ['ё'] = "yo",
    };

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
            result.Append(Transliterate(character));
        }
        return Tidy(result.ToString());
    }

    private static string Transliterate(char character)
    {
        if (character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_')
        {
            return character.ToString();
        }

        if (Latin.TryGetValue(char.ToLowerInvariant(character), out var latin))
        {
            return char.IsUpper(character) ? Capitalize(latin) : latin;
        }

        return IsApostrophe(character) ? "" : "_";
    }

    private static string Capitalize(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

    private static bool IsApostrophe(char character) => character is '\'' or '’' or 'ʼ';

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
