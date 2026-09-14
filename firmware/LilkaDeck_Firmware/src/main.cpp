#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "input/InputManager.h"
#include "display/DisplayManager.h"

USBHIDKeyboard Keyboard;
InputManager inputManager(Keyboard);
DisplayManager displayManager;

void setup() {
    Serial0.begin(115200);
    Serial0.println("\n--- LILKA BOOT SEQUENCE START ---");

    Serial0.println("[SYS] Starting USB...");
    Keyboard.begin();
    USB.begin();
    
    // Allow USB stack to stabilize and host OS to enumerate the HID device
    delay(200);

    Serial0.println("[SYS] Starting InputManager...");
    inputManager.begin();
    
    Serial0.println("[SYS] Starting DisplayManager...");
    displayManager.begin(); 

    Serial0.println("[SYS] Lilka Stream Deck: Ready.");
}

void loop() {
    // Process input non-blockingly to maintain HID responsiveness
    inputManager.update();
    
    // Yield CPU to prevent Watchdog Timer (WDT) triggers
    delay(1);
}
