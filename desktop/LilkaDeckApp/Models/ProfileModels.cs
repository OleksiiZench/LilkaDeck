namespace LilkaDeckApp.Models;

/// <summary>
/// Editor-side state of a single deck button, kept in the desktop application's memory.
/// </summary>
public class ButtonConfig
{
    public string IconPath { get; set; } = "";

    // Full path to the generated .raw file on the computer.
    public string IconFullPath { get; set; } = "";

    public bool NeedsUpload { get; set; } = false;

    public string ActionType { get; set; } = "shortcut";
    public string Actions { get; set; } = "";
}
