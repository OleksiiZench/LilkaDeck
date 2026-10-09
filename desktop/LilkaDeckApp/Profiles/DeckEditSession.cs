using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using LilkaDeckApp.Domain;

namespace LilkaDeckApp.Profiles;

/// <summary>What one save sends to the device: the profile's config.json and the icon files that changed.</summary>
public sealed record SyncPayload(byte[] ConfigJson, Dictionary<string, string> IconFiles);

/// <summary>
/// The profile being edited: the state of the eight buttons, plus the icon cache of that profile.
/// The editor sees it through <see cref="IButtonStore"/> and <see cref="IIconImporter"/>.
/// </summary>
public sealed class DeckEditSession : IButtonStore, IIconImporter
{
    private readonly ProfileCaches _caches;
    private readonly DeckState _state = new();
    
    // The text exactly as the editor last set it. Switching the action type back and forth must not
    // rewrite it, so a URL containing "," or "+" survives even while the type is briefly "shortcut".
    private readonly Dictionary<DeckPosition, string> _actionText = new();
    
    private IconCache _iconCache;
    private IconImporter _iconImporter;
    
    public DeckEditSession(ProfileCaches caches)
    {
        _caches = caches;
        ActivateProfile(0);
    }
    
    public ButtonEditState GetButton(DeckPosition position)
    {
        var definition = _state.Get(position).Definition;
        return new ButtonEditState(definition.IconFileName, definition.ActionType, ActionTextOf(position));
    }
    
    public void SetActionType(DeckPosition position, ActionType type) =>
    ChangeAction(position, type, ActionTextOf(position));
    
    public void SetActionText(DeckPosition position, string text) =>
    ChangeAction(position, _state.Get(position).Definition.ActionType, text);
    
    public void SetIcon(DeckPosition position, ImportedIcon icon) =>
    _state.Update(position, button => new DeckButtonState(
        button.Definition with { IconFileName = icon.FileName }, icon.CachedPath, IconNeedsUpload: true));
    
    /// <summary>Converts a picture into an icon in the current profile's cache.</summary>
    public ImportedIcon Import(string sourcePath) => _iconImporter.Import(sourcePath);
    
    /// <summary>The cached file of the button's icon, or null when it has none.</summary>
    public string? GetIconFilePath(DeckPosition position) => _state.Get(position).IconFilePath;
    
    public SyncPayload BuildSyncPayload(string profileName, string activeColor)
    {
        var document = _state.ToDocument(profileName, activeColor);
        return new SyncPayload(ProfileSerializer.Serialize(document), _state.CollectIconUploads(File.Exists));
    }
    
    /// <summary>Marks the icons that were just sent as uploaded.</summary>
    public void MarkUploaded(IReadOnlyDictionary<string, string> sentIcons) => _state.MarkUploaded(sentIcons);
    
    public void Clear()
    {
        _actionText.Clear();
        _state.Clear();
    }
    
    /// <summary>Makes a profile loaded from the device the current one; icons imported from now on go to its cache.</summary>
    public void ShowProfile(int profileId, ProfileDocument document)
    {
        ActivateProfile(profileId);
        _actionText.Clear();
        _state.Load(document, _iconCache.Find);
    }
    
    private string ActionTextOf(DeckPosition position)
    {
        if (_actionText.TryGetValue(position, out var text)) return text;
        
        var definition = _state.Get(position).Definition;
        return ActionTokens.ToText(definition.ActionType, definition.Actions);
    }
    
    private void ChangeAction(DeckPosition position, ActionType type, string text)
    {
        _actionText[position] = text;
        _state.Update(position, button => button with
        {
            Definition = button.Definition with { ActionType = type, Actions = ActionTokens.FromText(type, text) }
        });
    }
    
    [MemberNotNull(nameof(_iconCache), nameof(_iconImporter))]
    private void ActivateProfile(int profileId)
    {
        _iconCache = _caches.For(profileId);
        _iconImporter = new IconImporter(_iconCache);
    }
}
