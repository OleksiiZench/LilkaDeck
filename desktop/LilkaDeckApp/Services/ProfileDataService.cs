using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Models;
using LilkaDeckApp.Profiles;

namespace LilkaDeckApp.Services;

/// <summary>
/// Compatibility facade that keeps the string-based API MainWindow uses.
/// It can be deleted once MainWindow works with DeckState through view models.
/// </summary>
public class ProfileDataService : IButtonStore
{
    private readonly DeckState _state = new();

    // The text exactly as the editor last set it. Switching the action type back and forth must not
    // rewrite it, so a URL containing "," or "+" survives even while the type is briefly "shortcut".
    private readonly Dictionary<DeckPosition, string> _actionText = new();

    private IconCache _iconCache;
    private IconImporter _iconImporter;

    public ProfileDataService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Caches = new ProfileCaches(Path.Combine(appData, "LilkaDeck", "Cache"));
        ActivateProfile(0);
    }

    public ProfileCaches Caches { get; }

    public ButtonConfig GetConfig(string position) =>
        DeckPositions.TryParse(position, out var parsed) ? ToConfig(parsed) : new ButtonConfig();

    public void UpdateConfig(string position, Action<ButtonConfig> updateAction)
    {
        if (!DeckPositions.TryParse(position, out var parsed)) return;

        var config = ToConfig(parsed);
        updateAction(config);

        _actionText[parsed] = config.Actions;
        _state.Update(parsed, _ => FromConfig(config));
    }

    public ButtonEditState GetButton(DeckPosition position)
    {
        var config = ToConfig(position);
        return new ButtonEditState(config.IconPath, ActionTypes.Parse(config.ActionType), config.Actions);
    }

    public void SetActionType(DeckPosition position, ActionType type) =>
        UpdateConfig(position.ToWireName(), config => config.ActionType = type.ToWireName());

    public void SetActionText(DeckPosition position, string text) =>
        UpdateConfig(position.ToWireName(), config => config.Actions = text);

    public (byte[] jsonBytes, Dictionary<string, string> filesToSend) BuildSyncPayload(string profileName, string activeColorHex)
    {
        var document = _state.ToDocument(profileName, activeColorHex);
        return (ProfileSerializer.Serialize(document), _state.CollectIconUploads(File.Exists));
    }

    public void ClearState()
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

    /// <summary>Converts a picture into an icon in the current profile's cache. Throws <see cref="IconImportException"/> for unusable files.</summary>
    public ImportedIcon ImportIcon(string sourcePath) => _iconImporter.Import(sourcePath);

    /// <summary>Marks the icons that were just sent as uploaded.</summary>
    public void MarkUploaded(IReadOnlyDictionary<string, string> sentIcons) => _state.MarkUploaded(sentIcons);

    [MemberNotNull(nameof(_iconCache), nameof(_iconImporter))]
    private void ActivateProfile(int profileId)
    {
        _iconCache = Caches.For(profileId);
        _iconImporter = new IconImporter(_iconCache);
    }

    private ButtonConfig ToConfig(DeckPosition position)
    {
        var state = _state.Get(position);
        var definition = state.Definition;

        return new ButtonConfig
        {
            IconPath = definition.IconFileName,
            IconFullPath = state.IconFilePath ?? "",
            NeedsUpload = state.IconNeedsUpload,
            ActionType = definition.ActionType.ToWireName(),
            Actions = _actionText.TryGetValue(position, out var text)
                ? text
                : ActionTokens.ToText(definition.ActionType, definition.Actions)
        };
    }

    private static DeckButtonState FromConfig(ButtonConfig config)
    {
        var type = ActionTypes.Parse(config.ActionType);
        var definition = new ButtonDefinition(config.IconPath, type, ActionTokens.FromText(type, config.Actions));
        string? iconFilePath = string.IsNullOrWhiteSpace(config.IconFullPath) ? null : config.IconFullPath;
        return new DeckButtonState(definition, iconFilePath, config.NeedsUpload);
    }
}
