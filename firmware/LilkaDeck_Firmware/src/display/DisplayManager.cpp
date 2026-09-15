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

void DisplayManager::drawIcon(int32_t x, int32_t y, int32_t width, int32_t height, uint16_t* imageBuffer) {
    _tft.drawRect(x - 1, y - 1, width + 2, height + 2, TFT_WHITE);

    for (int j = 0; j < height; j++) {
        for (int i = 0; i < width; i++) {
            uint16_t pixel = imageBuffer[j * width + i];
            
            _tft.drawPixel(x + i, y + j, pixel);
        }
    }
}
