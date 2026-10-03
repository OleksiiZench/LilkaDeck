using System;
using System.Collections.Generic;
using System.Linq;

namespace LilkaDeckApp.Domain;

/// <summary>
/// What the device knows about one button: its icon file, the type of its action and the action tokens.
/// </summary>
public sealed record ButtonDefinition(string IconFileName, ActionType ActionType, IReadOnlyList<string> Actions)
{
    public static ButtonDefinition Empty { get; } = new("", ActionType.Shortcut, Array.Empty<string>());

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(IconFileName) && Actions.All(string.IsNullOrWhiteSpace);
}
