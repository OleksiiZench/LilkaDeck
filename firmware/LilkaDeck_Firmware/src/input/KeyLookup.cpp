#include "input/KeyLookup.h"
#include <USBHIDKeyboard.h>
#include <USBHIDConsumerControl.h>
#include "input/ExtendedKeys.h"

namespace {

struct KeyboardKey {
    const char* name;
    uint8_t keycode;
};

constexpr KeyboardKey KEYBOARD_KEYS[] = {
    // Modifiers
    {"CTRL", KEY_LEFT_CTRL}, {"SHIFT", KEY_LEFT_SHIFT}, {"ALT", KEY_LEFT_ALT}, {"GUI", KEY_LEFT_GUI},
    {"RCTRL", KEY_RIGHT_CTRL}, {"RSHIFT", KEY_RIGHT_SHIFT}, {"RALT", KEY_RIGHT_ALT}, {"RGUI", KEY_RIGHT_GUI},

    // Function keys
    {"F1", KEY_F1}, {"F2", KEY_F2}, {"F3", KEY_F3}, {"F4", KEY_F4},
    {"F5", KEY_F5}, {"F6", KEY_F6}, {"F7", KEY_F7}, {"F8", KEY_F8},
    {"F9", KEY_F9}, {"F10", KEY_F10}, {"F11", KEY_F11}, {"F12", KEY_F12},
    {"F13", KEY_F13}, {"F14", KEY_F14}, {"F15", KEY_F15}, {"F16", KEY_F16},
    {"F17", KEY_F17}, {"F18", KEY_F18}, {"F19", KEY_F19}, {"F20", KEY_F20},
    {"F21", KEY_F21}, {"F22", KEY_F22}, {"F23", KEY_F23}, {"F24", KEY_F24},

    // Navigation and editing
    {"UP", KEY_UP_ARROW}, {"DOWN", KEY_DOWN_ARROW}, {"LEFT", KEY_LEFT_ARROW}, {"RIGHT", KEY_RIGHT_ARROW},
    {"HOME", KEY_HOME}, {"END", KEY_END}, {"PAGEUP", KEY_PAGE_UP}, {"PAGEDOWN", KEY_PAGE_DOWN},
    {"INSERT", KEY_INSERT}, {"DELETE", KEY_DELETE}, {"BACKSPACE", KEY_BACKSPACE}, {"TAB", KEY_TAB},

    // Whitespace and control
    {"SPACE", ' '}, {"ENTER", KEY_RETURN}, {"ESC", KEY_ESC},

    // Lock and system keys
    {"CAPSLOCK", KEY_CAPS_LOCK}, {"NUMLOCK", ExtendedKey::NumLock}, {"SCROLLLOCK", ExtendedKey::ScrollLock},
    {"PRINTSCREEN", ExtendedKey::PrintScreen}, {"PAUSE", ExtendedKey::Pause}, {"MENU", ExtendedKey::Menu},

    // Punctuation
    {"MINUS", '-'}, {"EQUALS", '='}, {"COMMA", ','}, {"PERIOD", '.'}, {"SLASH", '/'}, {"SEMICOLON", ';'},
    {"QUOTE", '\''}, {"BACKSLASH", '\\'}, {"LBRACKET", '['}, {"RBRACKET", ']'}, {"GRAVE", '`'},

    // Numpad
    {"NUM0", ExtendedKey::Keypad0}, {"NUM1", ExtendedKey::Keypad1}, {"NUM2", ExtendedKey::Keypad2},
    {"NUM3", ExtendedKey::Keypad3}, {"NUM4", ExtendedKey::Keypad4}, {"NUM5", ExtendedKey::Keypad5},
    {"NUM6", ExtendedKey::Keypad6}, {"NUM7", ExtendedKey::Keypad7}, {"NUM8", ExtendedKey::Keypad8},
    {"NUM9", ExtendedKey::Keypad9},
    {"NUMPLUS", ExtendedKey::KeypadPlus}, {"NUMMINUS", ExtendedKey::KeypadMinus},
    {"NUMMULT", ExtendedKey::KeypadAsterisk}, {"NUMDIV", ExtendedKey::KeypadSlash},
    {"NUMDOT", ExtendedKey::KeypadDot}, {"NUMENTER", ExtendedKey::KeypadEnter},
};

struct MediaKey {
    const char* action;
    uint16_t usageCode;
};

constexpr MediaKey MEDIA_KEYS[] = {
    {"MEDIA_PLAY_PAUSE", CONSUMER_CONTROL_PLAY_PAUSE},
    {"MEDIA_NEXT", CONSUMER_CONTROL_SCAN_NEXT},
    {"MEDIA_PREV", CONSUMER_CONTROL_SCAN_PREVIOUS},
    {"MEDIA_VOL_UP", CONSUMER_CONTROL_VOLUME_INCREMENT},
    {"MEDIA_VOL_DOWN", CONSUMER_CONTROL_VOLUME_DECREMENT},
    {"MEDIA_MUTE", CONSUMER_CONTROL_MUTE},
    {"MEDIA_BRIGHTNESS_UP", CONSUMER_CONTROL_BRIGHTNESS_INCREMENT},
    {"MEDIA_BRIGHTNESS_DOWN", CONSUMER_CONTROL_BRIGHTNESS_DECREMENT},
};

// Letters and digits are written as single characters in the config, e.g. "A" or "5".
bool tryParseSingleCharacter(const String& name, uint8_t& outKeycode) {
    if (name.length() != 1) return false;

    char character = name.charAt(0);
    bool isUppercaseLetter = character >= 'A' && character <= 'Z';
    // The HID keyboard library expects letters in lowercase.
    outKeycode = isUppercaseLetter ? character + ('a' - 'A') : character;
    return true;
}

}

bool isMediaAction(const String& action) {
    return action.startsWith("MEDIA_");
}

bool tryLookupKeyboardKey(const String& name, uint8_t& outKeycode) {
    for (const KeyboardKey& key : KEYBOARD_KEYS) {
        if (name == key.name) {
            outKeycode = key.keycode;
            return true;
        }
    }
    return tryParseSingleCharacter(name, outKeycode);
}

bool tryLookupMediaKey(const String& action, uint16_t& outUsageCode) {
    for (const MediaKey& key : MEDIA_KEYS) {
        if (action == key.action) {
            outUsageCode = key.usageCode;
            return true;
        }
    }
    return false;
}
