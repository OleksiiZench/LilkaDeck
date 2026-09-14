#pragma once

#include <TFT_eSPI.h>

class DisplayManager {
public:
    DisplayManager();
    
    void begin();
    
    void clear();

private:
    TFT_eSPI _tft;
};
