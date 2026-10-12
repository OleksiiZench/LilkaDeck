using System.Collections.Generic;

namespace LilkaDeckApp.Domain;

/// <summary>
/// A profile as the device stores it: a name, the feedback color and the buttons that are set up.
/// </summary>
public sealed record ProfileDocument(
    string Name,
    string ActiveColor,
    IReadOnlyDictionary<DeckPosition, ButtonDefinition> Buttons)
{
    public const string DefaultName = "Profile";
    public const string DefaultActiveColor = "#00FFFF";

    /// <summary>An empty profile: the name and color defaults, no buttons. Used when the config on the device cannot be parsed.</summary>
    public static ProfileDocument CreateDefault() =>
        new(DefaultName, DefaultActiveColor, new Dictionary<DeckPosition, ButtonDefinition>());
}
