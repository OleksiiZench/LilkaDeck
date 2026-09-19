#include "display/DisplayManager.h"
#include "config/BoardConfig.h"
#include <Arduino.h>

DisplayManager::DisplayManager() : _tft() {
}

void DisplayManager::begin() {
    pinMode(BoardConfig::PIN_POWER_ENABLE, OUTPUT);
    digitalWrite(BoardConfig::PIN_POWER_ENABLE, HIGH);
    
    // Ensure the backlight is disabled before any memory operations
    const uint8_t PIN_BLK = 46;
    pinMode(PIN_BLK, OUTPUT);
    digitalWrite(PIN_BLK, LOW); 
    
    // Allow hardware voltage to stabilize
    delay(50); 

    // Initialize the ST7789 display controller
    _tft.init();

    // Bypass TFT_eSPI software clipping bounds to access the full physical SRAM.
    // The Lilka panel is 240x280, but the ST7789 chip retains 240x320 GRAM.
    // Resetting rotation to 0 aligns the coordinate origin with the hardware memory address 0.
    _tft.setRotation(0); 
    
    _tft.startWrite();
    // Open a write window mapping the entire 240x320 physical GRAM
    _tft.setWindow(0, 0, 240, 320); 
    // Push a continuous black block to overwrite all 76,800 pixels, eliminating factory retention noise
    _tft.pushBlock(TFT_BLACK, 240 * 320); 
    _tft.endWrite();

    // Safely restore the required landscape orientation
    _tft.setRotation(3);
    _tft.invertDisplay(true);
    
    // Execute a standard clear to synchronize internal TFT_eSPI state variables
    _tft.fillScreen(TFT_BLACK);
    
    // Allow the LCD matrix one frame cycle to update before illuminating
    delay(20); 
    
    // Enable the backlight now that the GRAM is fully sanitized
    digitalWrite(PIN_BLK, HIGH);
    
    Serial0.println("[TFT] Display initialized with full hardware GRAM wipe.");
}

void DisplayManager::clear() {
    // Dummy SPI transaction to stabilize the bus state after SD card operations
    _tft.startWrite();
    _tft.drawPixel(0, 0, TFT_BLACK);
    _tft.endWrite();

    _tft.fillScreen(TFT_BLACK);
}

SPIClass& DisplayManager::getSharedSpiBus() {
    return _tft.getSPIinstance();
}

void DisplayManager::drawIcon(IconPosition pos, uint16_t* imageBuffer) {
    int32_t x, y;
    getIconCoordinates(pos, x, y);

    // Dummy transaction for SPI bus synchronization
    _tft.startWrite();
    _tft.drawPixel(0, 0, TFT_BLACK);
    _tft.endWrite();

    // Endianness swap required for rendering standard RGB565 raw images
    _tft.setSwapBytes(true); 
    _tft.pushImage(x, y, 64, 64, imageBuffer);
}

void DisplayManager::showBootScreen()
{
    clear();

    _tft.setTextDatum(MC_DATUM);

    // Render primary title
    _tft.setTextColor(TFT_WHITE, TFT_BLACK);
    _tft.setTextSize(3); 
    _tft.drawString("LILKA DECK", _tft.width() / 2, _tft.height() / 2 - 15);

    // Render status subtitle
    _tft.setTextColor(TFT_DARKGREY, TFT_BLACK);
    _tft.setTextSize(1);
    _tft.drawString("Loading configuration...", _tft.width() / 2, _tft.height() / 2 + 25);
}

void DisplayManager::getIconCoordinates(IconPosition pos, int32_t& x, int32_t& y) {
    const int32_t ROW_UP = 18;
    const int32_t ROW_MID = 88;
    const int32_t ROW_DOWN = 158;

    switch (pos) {
        // Left D-Pad cluster alignment
        case IconPosition::LeftLeft:  x = 4;   y = ROW_MID;  break;
        case IconPosition::LeftUp:    x = 37;  y = ROW_UP;   break;
        case IconPosition::LeftDown:  x = 37;  y = ROW_DOWN; break;
        case IconPosition::LeftRight: x = 70;  y = ROW_MID;  break;

        // Right Action cluster alignment
        case IconPosition::RightLeft:  x = 146; y = ROW_MID;  break; 
        case IconPosition::RightUp:    x = 179; y = ROW_UP;   break;
        case IconPosition::RightDown:  x = 179; y = ROW_DOWN; break;
        case IconPosition::RightRight: x = 212; y = ROW_MID;  break;
    }
}
