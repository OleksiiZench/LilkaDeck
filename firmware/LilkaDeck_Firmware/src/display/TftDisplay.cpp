#include "display/TftDisplay.h"
#include "config/BoardConfig.h"
#include "display/IconGridLayout.h"
#include "util/Logger.h"

namespace {

constexpr const char* TAG = "TFT";

// The ST7789 keeps 240x320 of GRAM although the Lilka panel is only 240x280.
constexpr int32_t GRAM_WIDTH = 240;
constexpr int32_t GRAM_HEIGHT = 320;
constexpr uint8_t ROTATION_GRAM_ORIGIN = 0;
constexpr uint8_t ROTATION_LANDSCAPE = 3;

constexpr uint32_t POWER_STABILIZE_MS = 50;
constexpr uint32_t FRAME_SETTLE_MS = 20;

constexpr int32_t BOOT_TITLE_OFFSET_Y = -15;
constexpr int32_t BOOT_SUBTITLE_OFFSET_Y = 25;
constexpr uint8_t BOOT_TITLE_TEXT_SIZE = 3;

constexpr int32_t PROFILE_NAME_BAND_Y = 224;
constexpr int32_t PROFILE_NAME_BAND_WIDTH = 280;
constexpr int32_t PROFILE_NAME_BAND_HEIGHT = 16;
constexpr int32_t PROFILE_NAME_CENTER_X = 140;
constexpr int32_t PROFILE_NAME_BASELINE_Y = 238;

constexpr int32_t PRESS_BORDER_FIRST_INSET = 2;
constexpr int32_t PRESS_BORDER_LAST_INSET = 3;
constexpr int32_t PRESS_BORDER_RADIUS = 4;

void setBlkPin(bool high) {
    digitalWrite(BoardConfig::PIN_DISPLAY_BLK, high ? HIGH : LOW);
}

}

void TftDisplay::begin() {
    resetControllerViaBlkPin();
    delay(POWER_STABILIZE_MS);

    _tft.init();
    wipeGraphicsRam();
    configureLandscape();

    // Synchronizes TFT_eSPI's internal state with the wiped panel.
    _tft.fillScreen(TFT_BLACK);
    delay(FRAME_SETTLE_MS);

    setBlkPin(true);
    Log::info(TAG, "Display initialized with full GRAM wipe");
}

// Part of the boot-noise fix: toggling BLK makes the controller start from scratch.
void TftDisplay::resetControllerViaBlkPin() {
    pinMode(BoardConfig::PIN_DISPLAY_BLK, OUTPUT);
    setBlkPin(true);
    setBlkPin(false);
}

// Rotation 0 aligns the coordinate origin with GRAM address 0, bypassing
// TFT_eSPI clipping, so the whole 240x320 memory can be overwritten.
void TftDisplay::wipeGraphicsRam() {
    _tft.setRotation(ROTATION_GRAM_ORIGIN);

    _tft.startWrite();
    _tft.setWindow(0, 0, GRAM_WIDTH, GRAM_HEIGHT);
    _tft.pushBlock(TFT_BLACK, GRAM_WIDTH * GRAM_HEIGHT);
    _tft.endWrite();
}

void TftDisplay::configureLandscape() {
    _tft.setRotation(ROTATION_LANDSCAPE);
    _tft.invertDisplay(true);
}

// A dummy transaction re-asserts TFT_eSPI's SPI clock and mode,
// which the SD card may have changed on the shared bus.
void TftDisplay::stabilizeBus() {
    _tft.startWrite();
    _tft.drawPixel(0, 0, TFT_BLACK);
    _tft.endWrite();
}

SPIClass& TftDisplay::getSharedSpiBus() {
    return _tft.getSPIinstance();
}

void TftDisplay::showBootScreen() {
    clear();

    const int32_t centerX = _tft.width() / 2;
    const int32_t centerY = _tft.height() / 2;

    _tft.setTextDatum(MC_DATUM);

    _tft.setTextColor(TFT_WHITE, TFT_BLACK);
    _tft.setTextSize(BOOT_TITLE_TEXT_SIZE);
    _tft.drawString("LILKA DECK", centerX, centerY + BOOT_TITLE_OFFSET_Y);

    _tft.setTextColor(TFT_DARKGREY, TFT_BLACK);
    _tft.setTextSize(1);
    _tft.drawString("Loading configuration...", centerX, centerY + BOOT_SUBTITLE_OFFSET_Y);
}

void TftDisplay::clear() {
    stabilizeBus();
    _tft.fillScreen(TFT_BLACK);
}

void TftDisplay::drawIcon(IconPosition position, uint16_t* imageBuffer) {
    const ScreenPoint origin = IconGridLayout::originOf(position);
    stabilizeBus();

    // Raw icon files store RGB565 in the opposite byte order to the panel.
    _tft.setSwapBytes(true);
    _tft.pushImage(origin.x, origin.y, IconGridLayout::ICON_SIZE, IconGridLayout::ICON_SIZE, imageBuffer);
}

void TftDisplay::clearIconArea(IconPosition position) {
    const ScreenPoint origin = IconGridLayout::originOf(position);
    stabilizeBus();

    _tft.fillRect(origin.x, origin.y, IconGridLayout::ICON_SIZE, IconGridLayout::ICON_SIZE, TFT_BLACK);
}

void TftDisplay::setIconPressed(IconPosition position, bool isPressed, uint16_t color) {
    const ScreenPoint origin = IconGridLayout::originOf(position);
    const uint16_t borderColor = isPressed ? color : TFT_BLACK;
    stabilizeBus();

    _tft.startWrite();
    for (int32_t inset = PRESS_BORDER_FIRST_INSET; inset <= PRESS_BORDER_LAST_INSET; inset++) {
        const int32_t side = IconGridLayout::ICON_SIZE + inset * 2;
        _tft.drawRoundRect(origin.x - inset, origin.y - inset, side, side, PRESS_BORDER_RADIUS, borderColor);
    }
    _tft.endWrite();
}

void TftDisplay::drawProfileName(const String& name) {
    stabilizeBus();

    _tft.fillRect(0, PROFILE_NAME_BAND_Y, PROFILE_NAME_BAND_WIDTH, PROFILE_NAME_BAND_HEIGHT, TFT_BLACK);

    _tft.setTextDatum(BC_DATUM);
    _tft.setTextColor(TFT_LIGHTGREY, TFT_BLACK);
    _tft.setTextSize(1);
    _tft.drawString(name, PROFILE_NAME_CENTER_X, PROFILE_NAME_BASELINE_Y);
}
