#include "display/DisplayManager.h"
#include <Arduino.h>

// Hardware specific pin to keep the console powered / backlight enabled
constexpr uint8_t PIN_POWER_ENABLE = 46;

DisplayManager::DisplayManager() : _tft() {
}

void DisplayManager::begin() {
    // Manually assert power enable pin to prevent immediate shutdown
    pinMode(PIN_POWER_ENABLE, OUTPUT);
    digitalWrite(PIN_POWER_ENABLE, HIGH);
    delay(100);

    // Initialize SPI bus and the ST7789 display controller
    _tft.init();

    // Set landscape orientation (270 degrees) for the stream deck layout
    _tft.setRotation(3);
    
    clear();
    
    Serial0.println("[TFT] Display initialized.");
}

void DisplayManager::clear() {
    _tft.fillScreen(TFT_BLACK);
}
