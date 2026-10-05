using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Models;
using LilkaDeckApp.Profiles;

namespace LilkaDeckApp.Services;

/// <summary>
/// Compatibility facade that keeps the string-based API MainWindow uses.
/// It can be deleted once MainWindow works with DeckState through view models.
/// </summary>
public class ProfileDataService
{
    private readonly DeckState _state = new();
    private readonly IconCache _iconCache;
    private readonly IconImporter _iconImporter;

    // The text exactly as the editor last set it. Switching the action type back and forth must not
    // rewrite it, so a URL containing "," or "+" survives even while the type is briefly "shortcut".
    private readonly Dictionary<DeckPosition, string> _actionText = new();

    public ProfileDataService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _iconCache = new IconCache(Path.Combine(appData, "LilkaDeck", "Cache"));
        _iconImporter = new IconImporter(_iconCache);
    }

    public string CacheDirectory => _iconCache.DirectoryPath;

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

    public ProfileConfigDto? LoadFromJson(string jsonContent)
    {
        ClearState();

        if (!ProfileSerializer.TryDeserialize(jsonContent, out var document, out var error))
        {
            Trace.TraceWarning($"Cannot parse the profile config: {error}");
            return null;
        }

        _state.Load(document, _iconCache.Find);
        return ProfileSerializer.ToDto(document);
    }

    /// <summary>Converts a picture into an icon in the cache. Throws <see cref="IconImportException"/> for unusable files.</summary>
    public ImportedIcon ImportIcon(string sourcePath) => _iconImporter.Import(sourcePath);

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
