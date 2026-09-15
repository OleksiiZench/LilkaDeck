#pragma once
#include <TFT_eSPI.h>
#include <SPI.h>

class DisplayManager {
public:
    DisplayManager();
    
    void begin();
    void clear();
    
    // Expose the internal SPI bus for other peripherals (e.g., SD Card)
    SPIClass& getSharedSpiBus();

    void drawIcon(int32_t x, int32_t y, int32_t width, int32_t height, uint16_t* imageBuffer);

private:
    TFT_eSPI _tft;
};
