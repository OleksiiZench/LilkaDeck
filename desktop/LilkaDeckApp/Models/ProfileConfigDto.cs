using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LilkaDeckApp.Models;

/// <summary>
/// Root of config.json as stored on the device's SD card.
/// The property names are a contract with the firmware parser.
/// </summary>
public class ProfileConfigDto
{
    [JsonPropertyName("profileName")]
    public string ProfileName { get; set; } = "Profile";

    [JsonPropertyName("activeColor")]
    public string ActiveColor { get; set; } = "#00FFFF";

    [JsonPropertyName("buttons")]
    public Dictionary<string, ButtonDto> Buttons { get; set; } = new();
}

public class ButtonDto
{
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "shortcut";

    [JsonPropertyName("action")]
    public List<string> Action { get; set; } = new();
}
