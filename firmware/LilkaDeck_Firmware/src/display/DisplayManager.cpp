#include "display/DisplayManager.h"
#include <Arduino.h>

DisplayManager::DisplayManager() : _tft() {
}

void DisplayManager::begin() {
    pinMode(46, OUTPUT);
    digitalWrite(46, HIGH);
    delay(100);

    Serial0.println("[TFT] Before tft.init()");
    _tft.init();
    Serial0.println("[TFT] After tft.init()");

    _tft.setRotation(3);
    _tft.fillScreen(TFT_BLACK);
    Serial0.println("[TFT] Display ready.");
}

void DisplayManager::clear() {
    _tft.fillScreen(TFT_BLACK);
}
