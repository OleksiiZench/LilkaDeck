#include "input/InputManager.h"

InputManager::InputManager(USBHIDKeyboard& keyboard) : _keyboard(keyboard) {
    // Initialize the array with pins (using INPUT_PULLUP, so default state is HIGH/true)
    _buttons[static_cast<int>(ButtonID::Up)]     = {ButtonID::Up,     38, 'w', true, true, 0};
    _buttons[static_cast<int>(ButtonID::Down)]   = {ButtonID::Down,   41, 's', true, true, 0};
    _buttons[static_cast<int>(ButtonID::Left)]   = {ButtonID::Left,   39, 'a', true, true, 0};
    _buttons[static_cast<int>(ButtonID::Right)]  = {ButtonID::Right,  40, 'd', true, true, 0};
    
    _buttons[static_cast<int>(ButtonID::A)]      = {ButtonID::A,      5,  '1', true, true, 0};
    _buttons[static_cast<int>(ButtonID::B)]      = {ButtonID::B,      6,  '2', true, true, 0};
    _buttons[static_cast<int>(ButtonID::C)]      = {ButtonID::C,      10, '3', true, true, 0};
    _buttons[static_cast<int>(ButtonID::D)]      = {ButtonID::D,      9,  '4', true, true, 0};
    
    _buttons[static_cast<int>(ButtonID::Select)] = {ButtonID::Select, 0,  '5', true, true, 0};
    _buttons[static_cast<int>(ButtonID::Start)]  = {ButtonID::Start,  4,  '6', true, true, 0};
}

void InputManager::begin() {
    for (int i = 0; i < static_cast<int>(ButtonID::Count); i++) {
        pinMode(_buttons[i].pin, INPUT_PULLUP);
    }
}

void InputManager::update() {
    uint32_t currentMillis = millis();

    for (int i = 0; i < static_cast<int>(ButtonID::Count); i++) {
        ButtonDef& btn = _buttons[i];
        
        // Read raw state (LOW = pressed, HIGH = released)
        bool reading = digitalRead(btn.pin);

        // If the state changed (due to noise or actual press)
        if (reading != btn.lastState) {
            btn.lastDebounceTime = currentMillis; // Reset the debounce timer
        }

        // If the reading has been stable longer than the debounce delay
        if ((currentMillis - btn.lastDebounceTime) > DEBOUNCE_DELAY_MS) {
            
            // If the actual stable state has changed
            if (reading != btn.currentState) {
                btn.currentState = reading;

                // Button is pressed (pulled to GND)
                if (btn.currentState == LOW) {
                    Serial.printf("Pressed GPIO %d | Sending HID: '%c'\n", btn.pin, btn.hidKey);
                    _keyboard.press(btn.hidKey);
                } 
                // Button is released
                else {
                    Serial.printf("Released GPIO %d\n", btn.pin);
                    _keyboard.release(btn.hidKey);
                }
            }
        }
        // Save the raw reading for the next iteration
        btn.lastState = reading;
    }
}
