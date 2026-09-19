#pragma once

#include <Arduino.h>
#include <USBHIDKeyboard.h>
#include "display/DisplayManager.h"
#include "config/ConfigManager.h"

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
    InputManager(USBHIDKeyboard& keyboard, ConfigManager& configManager);
    
    void begin();
    void update();

private:
    USBHIDKeyboard& _keyboard;
    ConfigManager& _configManager;

    ButtonDef _buttons[static_cast<int>(ButtonID::Count)];
    static const uint32_t DEBOUNCE_DELAY_MS = 20;

    // Maps a physical button to a logical UI grid position
    bool getIconPositionForButton(ButtonID btnId, IconPosition& outPos);
    
    // Translates configuration string values into USB HID modifier/key codes
    uint8_t stringToKeycode(const String& keyStr);
};
