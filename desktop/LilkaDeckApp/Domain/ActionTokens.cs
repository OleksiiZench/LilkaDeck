using System.Collections.Generic;
using System.Linq;

namespace LilkaDeckApp.Domain;

/// <summary>
/// Converts between the text kept in the editor and the list of tokens stored in config.json.
/// A shortcut is several key tokens; a launch action is a single target.
/// </summary>
public static class ActionTokens
{
    // The editor shows "CTRL + T" while older state used "CTRL, T", so both separators are accepted.
    private static readonly char[] ShortcutSeparators = { ',', '+' };

    public static List<string> FromText(ActionType type, string text) =>
        type == ActionType.Shortcut
            ? Clean(text.Split(ShortcutSeparators))
            : Clean(new[] { text });

    /// <summary>Trims every token and drops the ones that are blank.</summary>
    public static List<string> Clean(IEnumerable<string?> tokens) =>
        tokens.Select(token => token?.Trim() ?? "").Where(token => token.Length > 0).ToList();

    public static string ToText(ActionType type, IReadOnlyList<string> tokens) =>
        type == ActionType.Shortcut
            ? string.Join(", ", tokens)
            : tokens.FirstOrDefault() ?? "";
}
