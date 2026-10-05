using System.Collections.Generic;
using System.Text;

namespace LilkaDeckApp.Imaging;

/// <summary>
/// Builds the file name an icon gets on the device. Names stay plain ASCII, because the SD card
/// stores them and the command that announces a file has to carry them unchanged.
/// </summary>
public static class IconFileName
{
    private const string Extension = ".raw";
    private const string FallbackStem = "icon";
    private const int MaxStemLength = 40;

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

    /// <summary>Turns the name of a picture, without its extension, into a device-safe icon file name.</summary>
    public static string FromSource(string sourceName)
    {
        var stem = new StringBuilder();
        foreach (char character in sourceName)
        {
            stem.Append(Transliterate(character));
        }
        return Tidy(stem.ToString()) + Extension;
    }

    private static string Transliterate(char character)
    {
        if (character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-')
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
