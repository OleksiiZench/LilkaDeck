#pragma once

#include <Arduino.h>
#include <USBHIDKeyboard.h>
#include "display/DisplayManager.h"
#include "config/ConfigManager.h"

// Enum for readable button identification
enum class ButtonID {
    Up = 0, Down, Left, Right,
    A, B, C, D,
    Select, Start,
    Count // Automatically keeps track of the total number of buttons (10)
};

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

    // Array size is automatically determined by the enum
    ButtonDef _buttons[static_cast<int>(ButtonID::Count)];
    static const uint32_t DEBOUNCE_DELAY_MS = 20;

    // Helper methods
    bool getIconPositionForButton(ButtonID btnId, IconPosition& outPos);
    uint8_t stringToKeycode(const String& keyStr);
};
