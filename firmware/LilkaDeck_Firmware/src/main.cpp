#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "config/BoardConfig.h"
#include "config/ConfigManager.h"
#include "input/InputManager.h"
#include "display/DisplayManager.h"
#include "storage/StorageManager.h"
#include "core/ProfileManager.h"
#include "core/SyncManager.h"

USBHIDKeyboard Keyboard;
ConfigManager configManager;
DisplayManager displayManager;
StorageManager storageManager;
ProfileManager profileManager(configManager, storageManager, displayManager);
InputManager inputManager(Keyboard, configManager, displayManager, profileManager);

SyncManager syncManager(storageManager, profileManager);

void setup() {
    pinMode(46, OUTPUT);
    digitalWrite(46, LOW);

    syncManager.begin();
    
    Serial.println("\n--- LILKA BOOT SEQUENCE START ---");

    pinMode(BoardConfig::PIN_SD_CS, OUTPUT);
    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);

    Serial.println("[SYS] Starting USB HID...");
    Keyboard.begin();
    USB.begin();
    delay(200);

    Serial.println("[SYS] Starting DisplayManager...");
    displayManager.begin();
    displayManager.showBootScreen();
    delay(1000);

    Serial.println("[SYS] Starting StorageManager...");
    SPIClass& sharedSpiBus = displayManager.getSharedSpiBus();

    if (storageManager.begin(sharedSpiBus, BoardConfig::PIN_SD_CS)) {
        Serial.println("[SYS] Booting ProfileManager...");
        profileManager.begin();
    } else {
        Serial.println("[ERR] SD Card mount failed.");
    }

    Serial.println("[SYS] Starting InputManager...");
    inputManager.begin();

    Serial.println("[SYS] Lilka Stream Deck: Ready.");
}

void loop() {
    syncManager.update();
    
    if (!syncManager.isBusy()) {
        inputManager.update();
        delay(1);
    }
}
