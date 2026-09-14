#pragma once
#include <Arduino.h>
#include <USBHIDKeyboard.h>

// Enum for readable button identification
enum class ButtonID {
    Up = 0, Down, Left, Right,
    A, B, C, D,
    Select, Start,
    Count // Automatically keeps track of the number of buttons (10)
};

// Structure to hold button configuration and state
struct ButtonDef {
    ButtonID id;
    uint8_t pin;
    uint8_t hidKey;          // The character/key to send via USB
    bool lastState;          // Raw state from the previous loop
    bool currentState;       // Debounced state
    uint32_t lastDebounceTime; 
};

class InputManager {
public:
    // Pass the Keyboard object by reference so the manager can use it
    InputManager(USBHIDKeyboard& keyboard);
    
    // Initialize pins
    void begin();
    
    // Process button states (must be called in loop)
    void update();

private:
    USBHIDKeyboard& _keyboard;
    static const uint32_t DEBOUNCE_DELAY_MS = 20; // 20 milliseconds debounce
    ButtonDef _buttons[static_cast<int>(ButtonID::Count)];
};
