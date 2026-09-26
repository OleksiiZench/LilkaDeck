using Avalonia.Input;
using System.Collections.Generic;

namespace LilkaDeckApp.Services;

/// <summary>
/// Maps physical Avalonia keys to the canonical string tokens understood by the ESP32 firmware.
/// This is the single source of truth for the desktop side of the "translation" —
/// add a new key here once, and it works everywhere without touching firmware.
/// </summary>
public static class KeyCaptureMap
{
    public static readonly Dictionary<Key, string> Map = new()
    {
        // Letters
        { Key.A, "A" }, { Key.B, "B" }, { Key.C, "C" }, { Key.D, "D" }, { Key.E, "E" },
        { Key.F, "F" }, { Key.G, "G" }, { Key.H, "H" }, { Key.I, "I" }, { Key.J, "J" },
        { Key.K, "K" }, { Key.L, "L" }, { Key.M, "M" }, { Key.N, "N" }, { Key.O, "O" },
        { Key.P, "P" }, { Key.Q, "Q" }, { Key.R, "R" }, { Key.S, "S" }, { Key.T, "T" },
        { Key.U, "U" }, { Key.V, "V" }, { Key.W, "W" }, { Key.X, "X" }, { Key.Y, "Y" },
        { Key.Z, "Z" },

        // Digits (top row)
        { Key.D0, "0" }, { Key.D1, "1" }, { Key.D2, "2" }, { Key.D3, "3" }, { Key.D4, "4" },
        { Key.D5, "5" }, { Key.D6, "6" }, { Key.D7, "7" }, { Key.D8, "8" }, { Key.D9, "9" },

        // Function keys F1-F24 (most keyboards only have F1-F12, but some have more)
        { Key.F1, "F1" }, { Key.F2, "F2" }, { Key.F3, "F3" }, { Key.F4, "F4" },
        { Key.F5, "F5" }, { Key.F6, "F6" }, { Key.F7, "F7" }, { Key.F8, "F8" },
        { Key.F9, "F9" }, { Key.F10, "F10" }, { Key.F11, "F11" }, { Key.F12, "F12" },
        { Key.F13, "F13" }, { Key.F14, "F14" }, { Key.F15, "F15" }, { Key.F16, "F16" },
        { Key.F17, "F17" }, { Key.F18, "F18" }, { Key.F19, "F19" }, { Key.F20, "F20" },
        { Key.F21, "F21" }, { Key.F22, "F22" }, { Key.F23, "F23" }, { Key.F24, "F24" },

        // Navigation / editing
        { Key.Up, "UP" }, { Key.Down, "DOWN" }, { Key.Left, "LEFT" }, { Key.Right, "RIGHT" },
        { Key.Home, "HOME" }, { Key.End, "END" }, { Key.PageUp, "PAGEUP" }, { Key.PageDown, "PAGEDOWN" },
        { Key.Insert, "INSERT" }, { Key.Delete, "DELETE" }, { Key.Back, "BACKSPACE" }, { Key.Tab, "TAB" },

        // Whitespace / control
        { Key.Space, "SPACE" }, { Key.Return, "ENTER" }, { Key.Escape, "ESC" },

        // Lock / system keys
        { Key.CapsLock, "CAPSLOCK" },
        { Key.NumLock, "NUMLOCK" },
        { Key.Scroll, "SCROLLLOCK" },
        { Key.PrintScreen, "PRINTSCREEN" },
        { Key.Pause, "PAUSE" },
        { Key.Apps, "MENU" }, // Контекстне меню (клавіша поруч з правим Ctrl)

        // Punctuation
        { Key.OemMinus, "MINUS" }, { Key.OemPlus, "EQUALS" },
        { Key.OemComma, "COMMA" }, { Key.OemPeriod, "PERIOD" },
        { Key.OemQuestion, "SLASH" }, { Key.OemSemicolon, "SEMICOLON" },
        { Key.OemQuotes, "QUOTE" }, { Key.OemBackslash, "BACKSLASH" },
        { Key.OemOpenBrackets, "LBRACKET" }, { Key.OemCloseBrackets, "RBRACKET" },
        { Key.OemTilde, "GRAVE" },

        // Numpad
        { Key.NumPad0, "NUM0" }, { Key.NumPad1, "NUM1" }, { Key.NumPad2, "NUM2" },
        { Key.NumPad3, "NUM3" }, { Key.NumPad4, "NUM4" }, { Key.NumPad5, "NUM5" },
        { Key.NumPad6, "NUM6" }, { Key.NumPad7, "NUM7" }, { Key.NumPad8, "NUM8" },
        { Key.NumPad9, "NUM9" },
        { Key.Add, "NUMPLUS" }, { Key.Subtract, "NUMMINUS" },
        { Key.Multiply, "NUMMULT" }, { Key.Divide, "NUMDIV" },
        { Key.Decimal, "NUMDOT" }, { Key.OemClear, "NUMCLEAR" },

        // Media keys (handled by USBHIDConsumerControl on the firmware side)
        { Key.MediaPlayPause, "MEDIA_PLAY_PAUSE" },
        { Key.MediaNextTrack, "MEDIA_NEXT" },
        { Key.MediaPreviousTrack, "MEDIA_PREV" },
        { Key.VolumeUp, "MEDIA_VOL_UP" },
        { Key.VolumeDown, "MEDIA_VOL_DOWN" },
        { Key.VolumeMute, "MEDIA_MUTE" },
    };

    /// <summary>
    /// Modifier keys are held, not "pressed" as the main key of a combo — captured separately
    /// via KeyModifiers in the KeyDown handler, so they're excluded from the main lookup above.
    /// Left/Right variants are distinguished here in case a combo needs a specific side
    /// (e.g. right Alt / AltGr on some layouts).
    /// </summary>
    public static readonly HashSet<Key> ModifierKeys = new()
    {
        Key.LeftCtrl, Key.RightCtrl,
        Key.LeftShift, Key.RightShift,
        Key.LeftAlt, Key.RightAlt,
        Key.LWin, Key.RWin
    };

    /// <summary>
    /// Explicit left/right modifier tokens, used only when the person holds a modifier
    /// as the single "main" key being captured (rare, but completes the picture).
    /// </summary>
    public static readonly Dictionary<Key, string> ExplicitModifierTokens = new()
    {
        { Key.LeftCtrl, "CTRL" }, { Key.RightCtrl, "RCTRL" },
        { Key.LeftShift, "SHIFT" }, { Key.RightShift, "RSHIFT" },
        { Key.LeftAlt, "ALT" }, { Key.RightAlt, "RALT" },
        { Key.LWin, "GUI" }, { Key.RWin, "RGUI" },
    };
}
