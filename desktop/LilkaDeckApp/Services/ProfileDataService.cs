using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LilkaDeckApp.Domain;
using LilkaDeckApp.Models;

namespace LilkaDeckApp.Services;

/// <summary>
/// Service responsible for managing the state of the deck configuration
/// and building the final payload for synchronization.
/// </summary>
public class ProfileDataService
{
    private const string DefaultProfileName = "Profile";

    private readonly Dictionary<DeckPosition, ButtonConfig> _deckConfigs = new();

    public string CacheDirectory { get; }

    public ProfileDataService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        CacheDirectory = Path.Combine(appData, "LilkaDeck", "Cache");
        Directory.CreateDirectory(CacheDirectory);

        ClearState();
    }

    /// <summary>
    /// Retrieves the configuration for a specific button position.
    /// </summary>
    public ButtonConfig GetConfig(string position)
    {
        return DeckPositions.TryParse(position, out var parsed) ? _deckConfigs[parsed] : new ButtonConfig();
    }

    /// <summary>
    /// Updates properties of a specific button configuration safely.
    /// </summary>
    public void UpdateConfig(string position, Action<ButtonConfig> updateAction)
    {
        if (DeckPositions.TryParse(position, out var parsed))
        {
            updateAction(_deckConfigs[parsed]);
        }
    }

    /// <summary>
    /// Compiles the current UI state into the final JSON byte array and the list of raw image files.
    /// </summary>
    public (byte[] jsonBytes, Dictionary<string, string> filesToSend) BuildSyncPayload(string profileName, string activeColorHex)
    {
        var output = new ProfileConfigDto
        {
            ProfileName = string.IsNullOrWhiteSpace(profileName) ? DefaultProfileName : profileName,
            ActiveColor = activeColorHex
        };

        var filesToSend = new Dictionary<string, string>();

        foreach (var (position, config) in _deckConfigs)
        {
            if (IsEmpty(config)) continue;

            output.Buttons[position.ToWireName()] = ToDto(config);

            if (ShouldUploadIcon(config))
            {
                filesToSend[config.IconPath] = config.IconFullPath;
            }
        }

        string jsonString = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
        return (System.Text.Encoding.UTF8.GetBytes(jsonString), filesToSend);
    }

    /// <summary>
    /// Clears the current state of the UI
    /// </summary>
    public void ClearState()
    {
        foreach (var position in DeckPositions.All)
        {
            _deckConfigs[position] = new ButtonConfig();
        }
    }

    /// <summary>
    /// Parses the JSON received from Lilka and updates the status
    /// </summary>
    public ProfileConfigDto? LoadFromJson(string jsonContent)
    {
        try
        {
            ClearState();
            var config = JsonSerializer.Deserialize<ProfileConfigDto>(jsonContent);
            if (config == null) return null;

            foreach (var (name, button) in config.Buttons)
            {
                if (DeckPositions.TryParse(name, out var position))
                {
                    ApplyDto(_deckConfigs[position], button);
                }
            }
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JSON Parse Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Copies the selected image to the cache before sending it (so it won't be lost)
    /// </summary>
    public string CacheImage(string originalPath, string fileName)
    {
        string cachedPath = Path.Combine(CacheDirectory, fileName);
        if (originalPath != cachedPath)
        {
            File.Copy(originalPath, cachedPath, true);
        }
        return cachedPath;
    }

    private static bool IsEmpty(ButtonConfig config) =>
        string.IsNullOrWhiteSpace(config.IconPath) && string.IsNullOrWhiteSpace(config.Actions);

    private static bool ShouldUploadIcon(ButtonConfig config) =>
        config.NeedsUpload && !string.IsNullOrWhiteSpace(config.IconFullPath) && File.Exists(config.IconFullPath);

    private static ButtonDto ToDto(ButtonConfig config)
    {
        var type = ActionTypes.Parse(config.ActionType);
        return new ButtonDto
        {
            Icon = config.IconPath,
            Type = type.ToWireName(),
            Action = ActionTokens.FromText(type, config.Actions)
        };
    }

    private void ApplyDto(ButtonConfig target, ButtonDto button)
    {
        var type = ActionTypes.Parse(button.Type);
        target.IconPath = button.Icon;
        target.ActionType = type.ToWireName();
        target.Actions = ActionTokens.ToText(type, button.Action);

        // An icon that is already in the cache does not need to be fetched or uploaded again.
        string cachedImage = Path.Combine(CacheDirectory, button.Icon);
        if (File.Exists(cachedImage))
        {
            target.IconFullPath = cachedImage;
            target.NeedsUpload = false;
        }
    }
}
