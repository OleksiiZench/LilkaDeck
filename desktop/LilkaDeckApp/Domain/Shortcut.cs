using System.Collections.Generic;

namespace LilkaDeckApp.Domain;

/// <summary>A key combination: the modifiers that were held, then the main key. Tokens are the firmware's key names.</summary>
public sealed record Shortcut(IReadOnlyList<string> Tokens)
{
    public static Shortcut Of(string mainKey, bool control, bool shift, bool alt, bool meta)
    {
        var tokens = new List<string>();
        if (control) tokens.Add("CTRL");
        if (shift) tokens.Add("SHIFT");
        if (alt) tokens.Add("ALT");
        if (meta) tokens.Add("GUI");
        tokens.Add(mainKey);
        return new Shortcut(tokens);
    }

    /// <summary>The form shown in the editor, for example "CTRL + ALT + T".</summary>
    public string ToDisplayText() => string.Join(" + ", Tokens);

    /// <summary>The form kept in the button's state.</summary>
    public string ToStoredText() => ActionTokens.ToText(ActionType.Shortcut, Tokens);
}
