#include "app/Application.h"
#include "USB.h"
#include "config/BoardConfig.h"
#include "util/Logger.h"

namespace {

constexpr const char* TAG = "SYS";
constexpr uint32_t USB_ENUMERATION_DELAY_MS = 200;
constexpr uint32_t BOOT_SCREEN_DURATION_MS = 1000;
constexpr uint32_t LOOP_IDLE_DELAY_MS = 1;

}

void Application::setup() {
    driveDisplayBlkLow();

    _serialLink.begin();
    Log::info(TAG, "--- LILKA BOOT SEQUENCE START ---");

    deselectSdCard();
    startUsb();
    startDisplay();
    startProfiles();
    startInput();

    Log::info(TAG, "Lilka Stream Deck: Ready.");
}

void Application::loop() {
    _sync.update();
    if (_sync.isBusy()) return;

    _input.update();
    delay(LOOP_IDLE_DELAY_MS);
}

void Application::driveDisplayBlkLow() {
    pinMode(BoardConfig::PIN_DISPLAY_BLK, OUTPUT);
    digitalWrite(BoardConfig::PIN_DISPLAY_BLK, LOW);
}

// Keeps the SD card deselected while the display initializes on the shared SPI bus.
void Application::deselectSdCard() {
    pinMode(BoardConfig::PIN_SD_CS, OUTPUT);
    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);
}

void Application::startUsb() {
    Log::info(TAG, "Starting USB HID...");
    _keyboard.begin();
    USB.begin();
    delay(USB_ENUMERATION_DELAY_MS);
}

void Application::startDisplay() {
    Log::info(TAG, "Starting display...");
    _display.begin();
    _display.showBootScreen();
    delay(BOOT_SCREEN_DURATION_MS);
}

void Application::startProfiles() {
    Log::info(TAG, "Starting storage...");
    if (!_fileSystem.begin(_display.getSharedSpiBus(), BoardConfig::PIN_SD_CS)) {
        Log::error(TAG, "SD card mount failed");
        return;
    }

    Log::info(TAG, "Booting ProfileManager...");
    _profiles.begin();
}

void Application::startInput() {
    Log::info(TAG, "Starting input...");
    _hid.begin();
    _input.begin();
}
