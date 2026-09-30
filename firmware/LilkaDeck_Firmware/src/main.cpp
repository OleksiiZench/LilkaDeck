#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "config/BoardConfig.h"
#include "config/ConfigManager.h"
#include "input/InputManager.h"
#include "display/DisplayManager.h"
#include "storage/SdFileSystem.h"
#include "storage/ProfileRepository.h"
#include "core/ProfileManager.h"
#include "core/SyncManager.h"
#include "util/Logger.h"

namespace {
constexpr const char* TAG = "SYS";
}

USBHIDKeyboard Keyboard;
ConfigManager configManager;
DisplayManager displayManager;
SdFileSystem sdFileSystem;
ProfileRepository profileRepository(sdFileSystem);
ProfileManager profileManager(configManager, profileRepository, displayManager);
InputManager inputManager(Keyboard, configManager, displayManager, profileManager);

SyncManager syncManager(profileRepository, profileManager);

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

    Log::info(TAG, "Starting storage...");
    SPIClass& sharedSpiBus = displayManager.getSharedSpiBus();

    if (sdFileSystem.begin(sharedSpiBus, BoardConfig::PIN_SD_CS)) {
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
