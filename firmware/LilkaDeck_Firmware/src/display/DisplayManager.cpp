#include "display/DisplayManager.h"
#include "config/BoardConfig.h"
#include <Arduino.h>

DisplayManager::DisplayManager() : _tft() {
}

void DisplayManager::begin() {
    // Manually assert power enable pin to prevent immediate shutdown
    pinMode(BoardConfig::PIN_POWER_ENABLE, OUTPUT);
    digitalWrite(BoardConfig::PIN_POWER_ENABLE, HIGH);
    delay(100);

    // Initializes SPI bus (HSPI) and the ST7789 display controller
    _tft.init();

    // Set landscape orientation (270 degrees) for the stream deck layout
    _tft.setRotation(3);

    _tft.invertDisplay(true);
    
    clear();
    
    Serial0.println("[TFT] Display initialized.");
}

void DisplayManager::clear() {
    _tft.fillScreen(TFT_BLACK);
}

SPIClass& DisplayManager::getSharedSpiBus() {
    return _tft.getSPIinstance();
}

void DisplayManager::drawIcon(IconPosition pos, uint16_t* imageBuffer) {
    int32_t x, y;
    getIconCoordinates(pos, x, y);

    _tft.startWrite();
    _tft.drawPixel(0, 0, TFT_BLACK);
    _tft.endWrite();

    _tft.setSwapBytes(true); 
    _tft.pushImage(x, y, 64, 64, imageBuffer);
}

void DisplayManager::getIconCoordinates(IconPosition pos, int32_t& x, int32_t& y) {
    // Base Y-coordinates for the three rows
    const int32_t ROW_UP = 18;
    const int32_t ROW_MID = 88;
    const int32_t ROW_DOWN = 158;

    switch (pos) {
        // --- LEFT SIDE ---
        case IconPosition::LeftLeft:  x = 4;   y = ROW_MID;  break;
        case IconPosition::LeftUp:    x = 37;  y = ROW_UP;   break;
        case IconPosition::LeftDown:  x = 37;  y = ROW_DOWN; break;
        case IconPosition::LeftRight: x = 70;  y = ROW_MID;  break;

        // --- RIGHT SIDE ---
        case IconPosition::RightLeft:  x = 146; y = ROW_MID;  break; 
        case IconPosition::RightUp:    x = 179; y = ROW_UP;   break;
        case IconPosition::RightDown:  x = 179; y = ROW_DOWN; break;
        case IconPosition::RightRight: x = 212; y = ROW_MID;  break;
    }
}
