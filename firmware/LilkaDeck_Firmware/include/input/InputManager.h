#pragma once

#include <Arduino.h>
#include <USBHIDKeyboard.h>
#include <USBHIDConsumerControl.h>
#include "display/DisplayManager.h"
#include "config/ConfigManager.h"

class ProfileManager;

// Extended keycode constants not present in USBHIDKeyboard.h.
// Follows the library's own encoding scheme: press() computes
// (value - 0x88) as the raw USB HID Usage ID before sending the report,
// so each constant below equals 0x88 + <standard HID Usage Table code>.
#define KEY_NUM_LOCK      0xDB
#define KEY_SCROLL_LOCK   0xCF
#define KEY_PRINT_SCREEN  0xCE
#define KEY_PAUSE         0xD0
#define KEY_MENU          0xED

#define KEY_KP_SLASH      0xDC
#define KEY_KP_ASTERISK   0xDD
#define KEY_KP_MINUS      0xDE
#define KEY_KP_PLUS       0xDF
#define KEY_KP_ENTER      0xE0
#define KEY_KP_1          0xE1
#define KEY_KP_2          0xE2
#define KEY_KP_3          0xE3
#define KEY_KP_4          0xE4
#define KEY_KP_5          0xE5
#define KEY_KP_6          0xE6
#define KEY_KP_7          0xE7
#define KEY_KP_8          0xE8
#define KEY_KP_9          0xE9
#define KEY_KP_0          0xEA
#define KEY_KP_DOT        0xEB

// Hardware abstraction for physical device buttons
enum class ButtonID {
    Up = 0, Down, Left, Right,
    A, B, C, D,
    Select, Start,
    Count 
};

// Represents the hardware state and properties of a single button
struct ButtonDef {
    ButtonID id;
    uint8_t pin;
    bool lastState;
    bool currentState;
    uint32_t lastDebounceTime;
};

class InputManager {
public:
    InputManager(USBHIDKeyboard& keyboard, ConfigManager& configManager, DisplayManager& displayManager, ProfileManager& profileManager);

    void begin();
    void update();

private:
    USBHIDKeyboard& _keyboard;
    USBHIDConsumerControl _mediaKeyboard;
    ConfigManager& _configManager;
    DisplayManager& _displayManager;
    ProfileManager& _profileManager;

    ButtonDef _buttons[static_cast<int>(ButtonID::Count)];
    static const uint32_t DEBOUNCE_DELAY_MS = 20;

    // Maps a physical button to a logical UI grid position
    bool getIconPositionForButton(ButtonID btnId, IconPosition& outPos);
    
    // Translates configuration string values into USB HID modifier/key codes
    uint8_t stringToKeycode(const String& keyStr);

    // Media/consumer-control actions use a separate USB HID page ("MEDIA_" prefix)
    bool isMediaAction(const String& action);
    void executeMediaAction(const String& action);
};
