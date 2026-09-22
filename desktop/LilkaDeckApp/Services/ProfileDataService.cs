using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LilkaDeckApp.Models;

namespace LilkaDeckApp.Services;

/// <summary>
/// Service responsible for managing the state of the deck configuration 
/// and building the final payload for synchronization.
/// </summary>
public class ProfileDataService
{
    private readonly Dictionary<string, ButtonConfig> _deckConfigs = new();

    public ProfileDataService()
    {
        // Initialize all physical buttons with empty configs
        string[] positions = { "LeftUp", "LeftLeft", "LeftRight", "LeftDown", "RightUp", "RightLeft", "RightRight", "RightDown" };
        foreach (var pos in positions)
        {
            _deckConfigs[pos] = new ButtonConfig();
        }
    }

    /// <summary>
    /// Retrieves the configuration for a specific button position.
    /// </summary>
    public ButtonConfig GetConfig(string position)
    {
        return _deckConfigs.TryGetValue(position, out var config) ? config : new ButtonConfig();
    }

    /// <summary>
    /// Updates properties of a specific button configuration safely.
    /// </summary>
    public void UpdateConfig(string position, Action<ButtonConfig> updateAction)
    {
        if (_deckConfigs.TryGetValue(position, out var config))
        {
            updateAction(config);
        }
    }

    /// <summary>
    /// Compiles the current UI state into the final JSON byte array and the list of raw image files.
    /// </summary>
    public (byte[] jsonBytes, Dictionary<string, string> filesToSend) BuildSyncPayload(string profileName, string activeColorHex)
    {
        var output = new OutputConfig
        {
            ProfileName = string.IsNullOrWhiteSpace(profileName) ? "Profile" : profileName,
            ActiveColor = activeColorHex
        };

        var filesToSend = new Dictionary<string, string>();

        foreach (var kvp in _deckConfigs)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value.IconPath) || !string.IsNullOrWhiteSpace(kvp.Value.Actions))
            {
                // Parse actions (comma separated for shortcuts, single string for launch)
                var actionList = kvp.Value.ActionType == "shortcut"
                    ? kvp.Value.Actions.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                    : new List<string> { kvp.Value.Actions.Trim() };

                output.Buttons[kvp.Key] = new OutputButton
                {
                    Icon = kvp.Value.IconPath,
                    Type = kvp.Value.ActionType,
                    Action = actionList
                };

                // Track local files that need to be sent over USB
                if (!string.IsNullOrWhiteSpace(kvp.Value.IconFullPath) && File.Exists(kvp.Value.IconFullPath))
                {
                    filesToSend[kvp.Value.IconPath] = kvp.Value.IconFullPath;
                }
            }
        }

        string jsonString = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

        return (jsonBytes, filesToSend);
    }
}
