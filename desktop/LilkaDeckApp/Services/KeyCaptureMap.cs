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

        // Function keys
        { Key.F1, "F1" }, { Key.F2, "F2" }, { Key.F3, "F3" }, { Key.F4, "F4" },
        { Key.F5, "F5" }, { Key.F6, "F6" }, { Key.F7, "F7" }, { Key.F8, "F8" },
        { Key.F9, "F9" }, { Key.F10, "F10" }, { Key.F11, "F11" }, { Key.F12, "F12" },

        // Navigation / editing
        { Key.Up, "UP" }, { Key.Down, "DOWN" }, { Key.Left, "LEFT" }, { Key.Right, "RIGHT" },
        { Key.Home, "HOME" }, { Key.End, "END" }, { Key.PageUp, "PAGEUP" }, { Key.PageDown, "PAGEDOWN" },
        { Key.Insert, "INSERT" }, { Key.Delete, "DELETE" }, { Key.Back, "BACKSPACE" }, { Key.Tab, "TAB" },

        // Whitespace / control
        { Key.Space, "SPACE" }, { Key.Return, "ENTER" }, { Key.Escape, "ESC" },

        // Punctuation
        { Key.OemMinus, "MINUS" }, { Key.OemPlus, "EQUALS" },
        { Key.OemComma, "COMMA" }, { Key.OemPeriod, "PERIOD" },
        { Key.OemQuestion, "SLASH" }, { Key.OemSemicolon, "SEMICOLON" },
        { Key.OemQuotes, "QUOTE" }, { Key.OemBackslash, "BACKSLASH" },
        { Key.OemOpenBrackets, "LBRACKET" }, { Key.OemCloseBrackets, "RBRACKET" },
        { Key.OemTilde, "GRAVE" },

        // Media keys (handled by USBHIDConsumerControl on the firmware side)
        { Key.MediaPlayPause, "MEDIA_PLAY_PAUSE" },
        { Key.MediaNextTrack, "MEDIA_NEXT" },
        { Key.MediaPreviousTrack, "MEDIA_PREV" },
        { Key.VolumeUp, "MEDIA_VOL_UP" },
        { Key.VolumeDown, "MEDIA_VOL_DOWN" },
        { Key.VolumeMute, "MEDIA_MUTE" },
    };

    public static readonly HashSet<Key> ModifierKeys = new()
    {
        Key.LeftCtrl, Key.RightCtrl,
        Key.LeftShift, Key.RightShift,
        Key.LeftAlt, Key.RightAlt,
        Key.LWin, Key.RWin
    };
}
