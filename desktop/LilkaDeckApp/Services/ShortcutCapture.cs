using Avalonia.Input;
using LilkaDeckApp.Domain;

namespace LilkaDeckApp.Services;

public enum CaptureOutcome
{
    Captured,
    /// <summary>Only a modifier key was pressed; the combination is not finished yet.</summary>
    ModifierOnly,
    /// <summary>The firmware has no token for this key.</summary>
    Unsupported
}

/// <summary>Turns a key press in the shortcut recorder into a combination.</summary>
public static class ShortcutCapture
{
    public static CaptureOutcome TryCapture(Key key, KeyModifiers modifiers, out Shortcut? shortcut)
    {
        shortcut = null;

        if (KeyCaptureMap.ModifierKeys.Contains(key)) return CaptureOutcome.ModifierOnly;
        if (!KeyCaptureMap.Map.TryGetValue(key, out string? mainKey)) return CaptureOutcome.Unsupported;

        shortcut = Shortcut.Of(
            mainKey,
            modifiers.HasFlag(KeyModifiers.Control),
            modifiers.HasFlag(KeyModifiers.Shift),
            modifiers.HasFlag(KeyModifiers.Alt),
            modifiers.HasFlag(KeyModifiers.Meta));
        return CaptureOutcome.Captured;
    }
}
