#pragma once

#include <TFT_eSPI.h>
#include <SPI.h>

#include "domain/IconPosition.h"

class DisplayManager {
public:
    DisplayManager();
    
    void begin();
    void clear();
    
    // Exposes the internal SPI bus instance for shared peripherals (e.g., SD Card)
    SPIClass& getSharedSpiBus();

    void drawIcon(IconPosition pos, uint16_t* imageBuffer);
    void showBootScreen();
    
    // Now requires a dynamic color parameter instead of using a hardcoded value
    void setIconPressed(IconPosition pos, bool isPressed, uint16_t color);
    
    void drawProfileName(const String& name);
    void clearIconArea(IconPosition pos);

private:
    TFT_eSPI _tft;

    // Translates logical grid positions to physical screen coordinates
    void getIconCoordinates(IconPosition pos, int32_t &x, int32_t &y);
};
