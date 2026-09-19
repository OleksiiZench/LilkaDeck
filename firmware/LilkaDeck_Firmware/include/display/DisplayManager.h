#pragma once
#include <TFT_eSPI.h>
#include <SPI.h>

// Defines logical grid positions for rendering UI elements
enum class IconPosition {
    LeftUp, LeftLeft, LeftRight, LeftDown,
    RightUp, RightLeft, RightRight, RightDown
};

class DisplayManager {
public:
    DisplayManager();
    
    void begin();
    void clear();
    
    // Exposes the internal SPI bus instance for shared peripherals (e.g., SD Card)
    SPIClass& getSharedSpiBus();

    void drawIcon(IconPosition pos, uint16_t* imageBuffer);
    void showBootScreen();

private:
    TFT_eSPI _tft;

    // Translates logical grid positions to physical screen coordinates
    void getIconCoordinates(IconPosition pos, int32_t &x, int32_t &y);
};
