#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "config/BoardConfig.h"
#include "config/ConfigManager.h"
#include "input/InputManager.h"
#include "display/DisplayManager.h"
#include "storage/StorageManager.h"
#include "core/ProfileManager.h"

USBHIDKeyboard Keyboard;
ConfigManager configManager;
DisplayManager displayManager;
StorageManager storageManager;
ProfileManager profileManager(configManager, storageManager, displayManager);
InputManager inputManager(Keyboard, configManager, displayManager, profileManager);

void setup() {
    pinMode(46, OUTPUT);
    digitalWrite(46, LOW);

    Serial0.begin(115200);
    Serial0.println("\n--- LILKA BOOT SEQUENCE START ---");

    pinMode(BoardConfig::PIN_SD_CS, OUTPUT);
    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);

    Serial0.println("[SYS] Starting USB HID...");
    Keyboard.begin();
    USB.begin();
    delay(200);

    Serial0.println("[SYS] Starting DisplayManager...");
    displayManager.begin();
    displayManager.showBootScreen();
    delay(1000);

    Serial0.println("[SYS] Starting StorageManager...");
    SPIClass& sharedSpiBus = displayManager.getSharedSpiBus();

    if (storageManager.begin(sharedSpiBus, BoardConfig::PIN_SD_CS)) {
        Serial0.println("[SYS] Booting ProfileManager...");
        profileManager.begin();
    } else {
        Serial0.println("[ERR] SD Card mount failed.");
    }

    Serial0.println("[SYS] Starting InputManager...");
    inputManager.begin();

    Serial0.println("[SYS] Lilka Stream Deck: Ready.");
}

void loop() {
    inputManager.update();
    delay(1);
}
