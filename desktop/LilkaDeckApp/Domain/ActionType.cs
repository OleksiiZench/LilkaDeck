namespace LilkaDeckApp.Domain;

public enum ActionType
{
    /// <summary>Keyboard shortcut or media key emulated over USB HID.</summary>
    Shortcut,

    /// <summary>Asks the desktop application to launch a program or open a URL.</summary>
    Launch
}

public static class ActionTypes
{
    private const string ShortcutWireName = "shortcut";
    private const string LaunchWireName = "launch";

    public static string ToWireName(this ActionType type) =>
        type == ActionType.Launch ? LaunchWireName : ShortcutWireName;

    // Anything other than "launch" behaves as a shortcut, which is how the firmware reads it.
    public static ActionType Parse(string? wireName) =>
        wireName == LaunchWireName ? ActionType.Launch : ActionType.Shortcut;
}
