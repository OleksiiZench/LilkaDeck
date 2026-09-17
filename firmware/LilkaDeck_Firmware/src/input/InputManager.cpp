#include "input/InputManager.h"

InputManager::InputManager(USBHIDKeyboard& keyboard, ConfigManager& configManager) 
    : _keyboard(keyboard), _configManager(configManager) {
    
    // Initialize the array with pins based on pinout.md
    // Using INPUT_PULLUP, so default unpressed state is HIGH (true)
    _buttons[static_cast<int>(ButtonID::Up)]     = {ButtonID::Up,     38, true, true, 0};
    _buttons[static_cast<int>(ButtonID::Down)]   = {ButtonID::Down,   41, true, true, 0};
    _buttons[static_cast<int>(ButtonID::Left)]   = {ButtonID::Left,   39, true, true, 0};
    _buttons[static_cast<int>(ButtonID::Right)]  = {ButtonID::Right,  40, true, true, 0};
    
    _buttons[static_cast<int>(ButtonID::A)]      = {ButtonID::A,      5,  true, true, 0};
    _buttons[static_cast<int>(ButtonID::B)]      = {ButtonID::B,      6,  true, true, 0};
    _buttons[static_cast<int>(ButtonID::C)]      = {ButtonID::C,      10, true, true, 0};
    _buttons[static_cast<int>(ButtonID::D)]      = {ButtonID::D,      9,  true, true, 0};
    
    _buttons[static_cast<int>(ButtonID::Select)] = {ButtonID::Select, 0,  true, true, 0};
    _buttons[static_cast<int>(ButtonID::Start)]  = {ButtonID::Start,  4,  true, true, 0};
}

void InputManager::begin() {
    // Configure all defined button pins with internal pull-up resistors
    for (int i = 0; i < static_cast<int>(ButtonID::Count); i++) {
        pinMode(_buttons[i].pin, INPUT_PULLUP);
    }
}

bool InputManager::getIconPositionForButton(ButtonID btnId, IconPosition& outPos) {
    // Maps physical buttons to logical screen grid positions
    switch (btnId) {
        // Left D-Pad cluster
        case ButtonID::Up:    outPos = IconPosition::LeftUp; return true;
        case ButtonID::Down:  outPos = IconPosition::LeftDown; return true;
        case ButtonID::Left:  outPos = IconPosition::LeftLeft; return true;
        case ButtonID::Right: outPos = IconPosition::LeftRight; return true;
        
        // Right Action Buttons cluster (mapped as C=Top, B=Bottom, D=Left, A=Right)
        case ButtonID::C:     outPos = IconPosition::RightUp; return true;
        case ButtonID::B:     outPos = IconPosition::RightDown; return true;
        case ButtonID::D:     outPos = IconPosition::RightLeft; return true;
        case ButtonID::A:     outPos = IconPosition::RightRight; return true;
        
        // Select and Start are system buttons, they do not trigger macros
        default: return false; 
    }
}

void InputManager::update() {
    uint32_t currentMillis = millis();

    for (int i = 0; i < static_cast<int>(ButtonID::Count); i++) {
        ButtonDef& btn = _buttons[i];
        
        // Read raw physical state (LOW = pressed, HIGH = released)
        bool reading = digitalRead(btn.pin);

        // Reset debounce timer if the state changed
        if (reading != btn.lastState) {
            btn.lastDebounceTime = currentMillis; 
        }

        // Evaluate if the state has been stable longer than the debounce threshold
        if ((currentMillis - btn.lastDebounceTime) > DEBOUNCE_DELAY_MS) {
            
            if (reading != btn.currentState) {
                btn.currentState = reading;

                // Button just pressed
                if (btn.currentState == LOW) {
                    IconPosition pos;
                    
                    // Check if this button is mapped to a macro position
                    if (getIconPositionForButton(btn.id, pos)) {
                        Serial0.printf("[INPUT] Pressed GPIO %d (Macro triggered)\n", btn.pin);
                        
                        const auto& configuredButtons = _configManager.getButtons();
                        auto it = configuredButtons.find(pos);
                        
                        if (it != configuredButtons.end()) {
                            const std::vector<String>& actions = it->second.actions;
                            
                            // Press all configured modifiers and keys simultaneously
                            for (const String& action : actions) {
                                uint8_t keycode = stringToKeycode(action);
                                if (keycode > 0) {
                                    _keyboard.press(keycode);
                                }
                            }
                        }
                    } else {
                        // System button pressed (Select/Start)
                        Serial0.printf("[INPUT] Pressed System GPIO %d\n", btn.pin);
                    }
                } 
                // Button just released
                else {
                    Serial0.printf("[INPUT] Released GPIO %d\n", btn.pin);
                    
                    // Always release all HID keys to prevent getting stuck
                    _keyboard.releaseAll();
                }
            }
        }
        
        // Persist the raw reading for the next loop iteration
        btn.lastState = reading;
    }
}

uint8_t InputManager::stringToKeycode(const String& keyStr) {
    // Standard modifier keys
    if (keyStr == "CTRL") return KEY_LEFT_CTRL;
    if (keyStr == "SHIFT") return KEY_LEFT_SHIFT;
    if (keyStr == "ALT") return KEY_LEFT_ALT;
    if (keyStr == "GUI") return KEY_LEFT_GUI; // Windows/Super key
    
    // Function keys
    if (keyStr == "F5") return KEY_F5;
    if (keyStr == "F12") return KEY_F12;
    
    // Explicit alphanumeric keys mapped for the current config
    if (keyStr == "M") return 'm';
    if (keyStr == "A") return 'a';
    if (keyStr == "T") return 't';
    if (keyStr == "B") return 'b';
    if (keyStr == "S") return 's';
    
    // Special control keys
    if (keyStr == "SPACE") return ' ';
    if (keyStr == "ENTER") return KEY_RETURN;
    if (keyStr == "ESC") return KEY_ESC;

    // Fallback parser for arbitrary single-character keys
    if (keyStr.length() == 1) {
        char c = keyStr.charAt(0);
        // USB HID expects lowercase ASCII for standard alphabetical keys
        if (c >= 'A' && c <= 'Z') {
            return c + 32; 
        }
        return c;
    }

    Serial0.printf("[WARN] Unmapped keycode string: %s\n", keyStr.c_str());
    return 0;
}
