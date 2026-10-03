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
}
