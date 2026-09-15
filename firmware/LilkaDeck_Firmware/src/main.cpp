#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "config/BoardConfig.h"
#include "input/InputManager.h"
#include "display/DisplayManager.h"
#include "storage/StorageManager.h"

USBHIDKeyboard Keyboard;
InputManager inputManager(Keyboard);
DisplayManager displayManager;
StorageManager storageManager;

void setup() {
    Serial0.begin(115200);
    Serial0.println("\n--- LILKA BOOT SEQUENCE START ---");

    // 1. Hardware Protection: Secure SPI bus state before any initialization
    // Keep SD deselected before the SPI bus is used by other peripherals.
    pinMode(BoardConfig::PIN_SD_CS, OUTPUT);
    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);

    Serial0.println("[SYS] Starting USB...");
    Keyboard.begin();
    USB.begin();
    
    // Allow USB stack to stabilize and host OS to enumerate the HID device
    delay(200);

    Serial0.println("[SYS] Starting InputManager...");
    inputManager.begin();
    
    Serial0.println("[SYS] Starting DisplayManager...");
    // Initializes TFT_eSPI and its configured SPI instance.
    displayManager.begin(); 

    Serial0.println("[SYS] Starting StorageManager...");
    // Explicit Dependency Injection: Share the initialized SPI bus with SD Card
    SPIClass& sharedSpiBus = displayManager.getSharedSpiBus();
    storageManager.begin(sharedSpiBus, BoardConfig::PIN_SD_CS);

    Serial0.println("[SYS] Lilka Stream Deck: Ready.");
}

void loop() {
    // Process input non-blockingly to maintain HID responsiveness
    inputManager.update();
    
    // Yield CPU to prevent Watchdog Timer (WDT) triggers
    delay(1);
}
