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

private:
    TFT_eSPI _tft;
};
