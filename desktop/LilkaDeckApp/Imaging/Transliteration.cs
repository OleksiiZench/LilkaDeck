using System.Collections.Generic;

namespace LilkaDeckApp.Imaging;

/// <summary>
/// Turns one character into plain ASCII for a name stored on the device. Ukrainian letters are
/// transliterated, Latin letters and digits are kept, and anything else becomes an underscore.
/// </summary>
internal static class Transliteration
{
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

    public static string Of(char character)
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
}
