using System;
using System.Collections.Generic;
using System.Linq;

namespace LilkaDeckApp.Domain;

/// <summary>
/// Logical position of a button on the deck. The member names are the wire names used
/// in config.json and by the firmware (IconPosition), so they must not be renamed.
/// </summary>
public enum DeckPosition
{
    LeftUp, LeftLeft, LeftRight, LeftDown,
    RightUp, RightLeft, RightRight, RightDown
}

public static class DeckPositions
{
    private static readonly Dictionary<string, DeckPosition> ByWireName =
        Enum.GetValues<DeckPosition>().ToDictionary(position => position.ToString());

    public static IReadOnlyList<DeckPosition> All { get; } = Enum.GetValues<DeckPosition>();

    public static string ToWireName(this DeckPosition position) => position.ToString();

    // Unlike Enum.TryParse, this rejects numbers, comma lists and wrong casing.
    public static bool TryParse(string? wireName, out DeckPosition position)
    {
        position = default;
        return wireName != null && ByWireName.TryGetValue(wireName, out position);
    }
}
