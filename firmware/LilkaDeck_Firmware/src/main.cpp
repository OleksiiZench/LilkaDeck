#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "config/BoardConfig.h"
#include "config/ConfigManager.h"
#include "display/TftDisplay.h"
#include "storage/SdFileSystem.h"
#include "storage/ProfileRepository.h"
#include "core/ProfileNavigator.h"
#include "core/ProfilePresenter.h"
#include "core/ProfileManager.h"
#include "sync/ArduinoSerialLink.h"
#include "sync/FileReceiver.h"
#include "sync/SyncManager.h"
#include "input/ActionExecutor.h"
#include "input/ButtonReader.h"
#include "input/InputController.h"
#include "input/UsbHidOutput.h"
#include "util/Logger.h"

namespace {
constexpr const char* TAG = "SYS";
}

USBHIDKeyboard Keyboard;
ConfigManager configManager;
TftDisplay display;
SdFileSystem sdFileSystem;
ProfileRepository profileRepository(sdFileSystem);
ProfileNavigator profileNavigator;
ProfilePresenter profilePresenter(configManager, profileRepository, display);
ProfileManager profileManager(configManager, profileRepository, profileNavigator, profilePresenter);

ArduinoSerialLink serialLink;
FileReceiver fileReceiver(serialLink);
SyncManager syncManager(serialLink, profileRepository, profileManager, fileReceiver);

UsbHidOutput hidOutput(Keyboard);
ActionExecutor actionExecutor(hidOutput, serialLink);
ButtonReader buttonReader;
InputController inputController(buttonReader, configManager, display, profileManager, actionExecutor);

void setup() {
    pinMode(BoardConfig::PIN_DISPLAY_BLK, OUTPUT);
    digitalWrite(BoardConfig::PIN_DISPLAY_BLK, LOW);

    serialLink.begin();
    Log::info(TAG, "--- LILKA BOOT SEQUENCE START ---");

    pinMode(BoardConfig::PIN_SD_CS, OUTPUT);
    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);

    Log::info(TAG, "Starting USB HID...");
    Keyboard.begin();
    USB.begin();
    delay(200);

    Log::info(TAG, "Starting display...");
    display.begin();
    display.showBootScreen();
    delay(1000);

    Log::info(TAG, "Starting storage...");
    SPIClass& sharedSpiBus = display.getSharedSpiBus();

    if (sdFileSystem.begin(sharedSpiBus, BoardConfig::PIN_SD_CS)) {
        Log::info(TAG, "Booting ProfileManager...");
        profileManager.begin();
    } else {
        Log::error(TAG, "SD card mount failed");
    }

    Log::info(TAG, "Starting input...");
    hidOutput.begin();
    inputController.begin();

    Log::info(TAG, "Lilka Stream Deck: Ready.");
}

void loop() {
    syncManager.update();
    if (!syncManager.isBusy()) {
        inputController.update();
        delay(1);
    }
}
