using System;
using System.Collections.Generic;
using System.Linq;
using LilkaDeckApp.Domain;

namespace LilkaDeckApp.Profiles;

/// <summary>
/// One button as the editor sees it: the definition the device gets, plus where its icon file
/// is on this computer and whether that file still has to be sent.
/// </summary>
public sealed record DeckButtonState(ButtonDefinition Definition, string? IconFilePath = null, bool IconNeedsUpload = false)
{
    public static DeckButtonState Empty { get; } = new(ButtonDefinition.Empty);
}

/// <summary>
/// The editable state of the whole deck: eight buttons, always present.
/// </summary>
public sealed class DeckState
{
    private readonly Dictionary<DeckPosition, DeckButtonState> _buttons = new();

    public DeckState()
    {
        Clear();
    }

    public DeckButtonState Get(DeckPosition position) => _buttons[position];

    public void Update(DeckPosition position, Func<DeckButtonState, DeckButtonState> change)
    {
        _buttons[position] = change(_buttons[position]);
    }

    public void Clear()
    {
        foreach (var position in DeckPositions.All)
        {
            _buttons[position] = DeckButtonState.Empty;
        }
    }

    /// <summary>Replaces the state with a loaded profile; icons found in the cache need no upload.</summary>
    public void Load(ProfileDocument document, Func<string, string?> findCachedIcon)
    {
        Clear();
        foreach (var (position, definition) in document.Buttons)
        {
            _buttons[position] = new DeckButtonState(definition, findCachedIcon(definition.IconFileName));
        }
    }

    public ProfileDocument ToDocument(string profileName, string activeColor)
    {
        string name = string.IsNullOrWhiteSpace(profileName) ? ProfileDocument.DefaultName : profileName;
        var buttons = DeckPositions.All.ToDictionary(position => position, position => _buttons[position].Definition);
        return new ProfileDocument(name, activeColor, buttons);
    }

    /// <summary>Icon files that still have to be sent to the device, keyed by their file name.</summary>
    public Dictionary<string, string> CollectIconUploads(Func<string, bool> fileExists)
    {
        var uploads = new Dictionary<string, string>();

        foreach (var position in DeckPositions.All)
        {
            var button = _buttons[position];
            if (button.Definition.IsEmpty || !button.IconNeedsUpload) continue;
            if (string.IsNullOrWhiteSpace(button.IconFilePath) || !fileExists(button.IconFilePath)) continue;

            uploads[button.Definition.IconFileName] = button.IconFilePath;
        }
        return uploads;
    }
}
