#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "input/InputManager.h"

USBHIDKeyboard Keyboard;
InputManager inputManager(Keyboard);

void setup() {
    Serial.begin(115200);
    
    // Start USB and HID profile
    Keyboard.begin();
    USB.begin();

    // Initialize buttons
    inputManager.begin();
    
    Serial.println("Lilka Stream Deck: Ready.");
}

void loop() {
    // Poll buttons non-blockingly
    inputManager.update();
    
    // Small delay to prevent watchdog triggers and yield CPU
    delay(1);
}