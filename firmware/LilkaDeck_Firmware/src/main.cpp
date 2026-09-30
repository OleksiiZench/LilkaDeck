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
#include "util/Logger.h"

namespace {
constexpr const char* TAG = "SYS";
}

USBHIDKeyboard Keyboard;
ConfigManager configManager;
DisplayManager displayManager;
StorageManager storageManager;
ProfileManager profileManager(configManager, storageManager, displayManager);
InputManager inputManager(Keyboard, configManager, displayManager, profileManager);

SyncManager syncManager(storageManager, profileManager);

void setup() {
    pinMode(BoardConfig::PIN_DISPLAY_BLK, OUTPUT);
    digitalWrite(BoardConfig::PIN_DISPLAY_BLK, LOW);

    syncManager.begin();
    Log::info(TAG, "--- LILKA BOOT SEQUENCE START ---");

    pinMode(BoardConfig::PIN_SD_CS, OUTPUT);
    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);

    Log::info(TAG, "Starting USB HID...");
    Keyboard.begin();
    USB.begin();
    delay(200);

    Log::info(TAG, "Starting DisplayManager...");
    displayManager.begin();
    displayManager.showBootScreen();
    delay(1000);

    Log::info(TAG, "Starting StorageManager...");
    SPIClass& sharedSpiBus = displayManager.getSharedSpiBus();

    if (storageManager.begin(sharedSpiBus, BoardConfig::PIN_SD_CS)) {
        Log::info(TAG, "Booting ProfileManager...");
        profileManager.begin();
    } else {
        Log::error(TAG, "SD card mount failed");
    }

    Log::info(TAG, "Starting InputManager...");
    inputManager.begin();

    Log::info(TAG, "Lilka Stream Deck: Ready.");
}

void loop() {
    syncManager.update();
    if (!syncManager.isBusy()) {
        inputManager.update();
        delay(1);
    }
}
