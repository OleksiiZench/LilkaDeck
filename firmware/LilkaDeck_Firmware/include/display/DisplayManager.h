#pragma once
#include <TFT_eSPI.h>
#include <SPI.h>

enum class IconPosition {
    LeftUp, LeftLeft, LeftRight, LeftDown,
    RightUp, RightLeft, RightRight, RightDown
};




class DisplayManager {
public:
    DisplayManager();
    
    void begin();
    void clear();
    
    // Expose the internal SPI bus for other peripherals (e.g., SD Card)
    SPIClass& getSharedSpiBus();

    void drawIcon(IconPosition pos, uint16_t* imageBuffer);

private:
    TFT_eSPI _tft;

    void getIconCoordinates(IconPosition pos, int32_t &x, int32_t &y);
};
