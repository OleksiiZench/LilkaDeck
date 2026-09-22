#include "input/InputManager.h"
#include "core/ProfileManager.h"

InputManager::InputManager(USBHIDKeyboard& keyboard, ConfigManager& configManager, DisplayManager& displayManager, ProfileManager& profileManager) 
    : _keyboard(keyboard), _configManager(configManager), _displayManager(displayManager), _profileManager(profileManager) {
    
    // Map GPIO pins to logical identifiers based on hardware schematics.
    // Pins utilize internal pull-ups; default unpressed state evaluates to HIGH (true).
    
    // Left Cluster (Directional Pad)
    _buttons[static_cast<int>(ButtonID::Up)]     = {ButtonID::Up,     38, true, true, 0};
    _buttons[static_cast<int>(ButtonID::Down)]   = {ButtonID::Down,   41, true, true, 0};
    _buttons[static_cast<int>(ButtonID::Left)]   = {ButtonID::Left,   39, true, true, 0};
    _buttons[static_cast<int>(ButtonID::Right)]  = {ButtonID::Right,  40, true, true, 0};
    
    // Right Cluster (Action Buttons)
    _buttons[static_cast<int>(ButtonID::A)]      = {ButtonID::A,      5,  true, true, 0};
    _buttons[static_cast<int>(ButtonID::B)]      = {ButtonID::B,      6,  true, true, 0};
    _buttons[static_cast<int>(ButtonID::C)]      = {ButtonID::C,      10, true, true, 0};
    _buttons[static_cast<int>(ButtonID::D)]      = {ButtonID::D,      9,  true, true, 0};
    
    // System Control Buttons
    _buttons[static_cast<int>(ButtonID::Select)] = {ButtonID::Select, 0,  true, true, 0};
    _buttons[static_cast<int>(ButtonID::Start)]  = {ButtonID::Start,  4,  true, true, 0};
}

void InputManager::begin() {
    for (int i = 0; i < static_cast<int>(ButtonID::Count); i++) {
        pinMode(_buttons[i].pin, INPUT_PULLUP);
    }
}

bool InputManager::getIconPositionForButton(ButtonID btnId, IconPosition& outPos) {
    switch (btnId) {
        case ButtonID::Up:    outPos = IconPosition::LeftUp; return true;
        case ButtonID::Down:  outPos = IconPosition::LeftDown; return true;
        case ButtonID::Left:  outPos = IconPosition::LeftLeft; return true;
        case ButtonID::Right: outPos = IconPosition::LeftRight; return true;
        
        case ButtonID::C:     outPos = IconPosition::RightUp; return true;
        case ButtonID::B:     outPos = IconPosition::RightDown; return true;
        case ButtonID::D:     outPos = IconPosition::RightLeft; return true;
        case ButtonID::A:     outPos = IconPosition::RightRight; return true;
        
        // System buttons (Select/Start) are isolated from standard macro execution
        default: return false; 
    }
}

void InputManager::update() {
    uint32_t currentMillis = millis();

    for (int i = 0; i < static_cast<int>(ButtonID::Count); i++) {
        ButtonDef& btn = _buttons[i];
        
        // Evaluate raw GPIO state (LOW = pressed due to active pull-up)
        bool reading = digitalRead(btn.pin);

        if (reading != btn.lastState) {
            btn.lastDebounceTime = currentMillis; 
        }

        // Process state change only if the signal remains stable beyond the debounce threshold
        if ((currentMillis - btn.lastDebounceTime) > DEBOUNCE_DELAY_MS) {
            
            if (reading != btn.currentState) {
                btn.currentState = reading;

                // Handle Falling Edge (Button Pressed)
                if (btn.currentState == LOW) {
                    IconPosition pos;
                    
                    if (getIconPositionForButton(btn.id, pos)) {
                        Serial.printf("[INPUT] Pressed GPIO %d (Macro triggered)\n", btn.pin);
                        
                        // Execute immediate visual feedback (green border)
                        _displayManager.setIconPressed(pos, true);

                        const auto& configuredButtons = _configManager.getButtons();
                        auto it = configuredButtons.find(pos);
                        
                        if (it != configuredButtons.end()) {
                            const ButtonConfig& btnConfig = it->second;
                            const std::vector<String>& actions = btnConfig.actions;
                            
                            // Hybrid Execution Logic
                            if (btnConfig.type == "launch") {
                                // Request the companion desktop app to launch the specified target
                                if (!actions.empty()) {
                                    String command = "EXECUTE:" + actions[0];
                                    Serial.println(command);
                                }
                            } else {
                                // Default HID Keyboard emulation
                                for (const String& action : actions) {
                                    uint8_t keycode = stringToKeycode(action);
                                    if (keycode > 0) {
                                        _keyboard.press(keycode);
                                    }
                                }
                            }
                        }
                    } else {
                        // System button pressed (Select/Start)
                        if (btn.id == ButtonID::Select) {
                            Serial.println("[INPUT] Select -> Previous Profile");
                            _profileManager.previousProfile();
                        } else if (btn.id == ButtonID::Start) {
                            Serial.println("[INPUT] Start -> Next Profile");
                            _profileManager.nextProfile();
                        }
                    }
                } 
                // Handle Rising Edge (Button Released)
                else {
                    Serial.printf("[INPUT] Released GPIO %d\n", btn.pin);
                    
                    IconPosition pos;
                    if (getIconPositionForButton(btn.id, pos)) {
                        // Remove visual feedback
                        _displayManager.setIconPressed(pos, false);
                    }
                    
                    // Globally clear HID report to prevent persistent phantom keystrokes on the host OS
                    // (This safely affects only keys pressed during a 'shortcut' action)
                    _keyboard.releaseAll();
                }
            }
        }
        
        btn.lastState = reading;
    }
}

uint8_t InputManager::stringToKeycode(const String& keyStr) {
    if (keyStr == "CTRL") return KEY_LEFT_CTRL;
    if (keyStr == "SHIFT") return KEY_LEFT_SHIFT;
    if (keyStr == "ALT") return KEY_LEFT_ALT;
    if (keyStr == "GUI") return KEY_LEFT_GUI; 
    
    if (keyStr == "F5") return KEY_F5;
    if (keyStr == "F12") return KEY_F12;
    
    if (keyStr == "M") return 'm';
    if (keyStr == "A") return 'a';
    if (keyStr == "T") return 't';
    if (keyStr == "B") return 'b';
    if (keyStr == "S") return 's';
    
    if (keyStr == "SPACE") return ' ';
    if (keyStr == "ENTER") return KEY_RETURN;
    if (keyStr == "ESC") return KEY_ESC;

    // Fallback parser: cast arbitrary single-character strings to valid USB HID ASCII bounds
    if (keyStr.length() == 1) {
        char c = keyStr.charAt(0);
        if (c >= 'A' && c <= 'Z') {
            return c + 32; 
        }
        return c;
    }

    Serial.printf("[WARN] Unmapped keycode string: %s\n", keyStr.c_str());
    return 0;
}
