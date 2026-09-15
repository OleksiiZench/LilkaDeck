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
    
    clear();
    
    Serial0.println("[TFT] Display initialized.");
}

SPIClass& DisplayManager::getSharedSpiBus() {
    return _tft.getSPIinstance();
}

void DisplayManager::clear() {
    _tft.fillScreen(TFT_BLACK);
}
