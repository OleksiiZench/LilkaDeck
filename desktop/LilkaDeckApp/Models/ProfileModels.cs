using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LilkaDeckApp.Models;

/// <summary>
/// Local model for storing the settings of each button in the desktop application's memory.
/// </summary>
public class ButtonConfig
{
    public string IconPath { get; set; } = "";
    
    // Full path to the generated .raw file on the computer. 
    public string IconFullPath { get; set; } = ""; 
    
    public string ActionType { get; set; } = "shortcut";
    public string Actions { get; set; } = "";
}

/// <summary>
/// Root model of the JSON configuration that will be saved to the microcontroller's SD card.
/// </summary>
public class OutputConfig
{
    [JsonPropertyName("profileName")]
    public string ProfileName { get; set; } = "Profile";

    [JsonPropertyName("activeColor")]
    public string ActiveColor { get; set; } = "#00FFFF"; 

    [JsonPropertyName("buttons")]
    public Dictionary<string, OutputButton> Buttons { get; set; } = new();
}

/// <summary>
/// Model of a single button for JSON.
/// </summary>
public class OutputButton
{
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "shortcut";

    [JsonPropertyName("action")]
    public List<string> Action { get; set; } = new();
}
