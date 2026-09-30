#pragma once

#include <TFT_eSPI.h>
#include <SPI.h>
#include "display/IDisplay.h"

class TftDisplay : public IDisplay {
public:
    void begin();
    void showBootScreen();
    // The SD card shares this SPI bus, and TFT_eSPI owns the underlying instance.
    SPIClass& getSharedSpiBus();

    void clear() override;
    void drawIcon(IconPosition position, uint16_t* imageBuffer) override;
    void clearIconArea(IconPosition position) override;
    void setIconPressed(IconPosition position, bool isPressed, uint16_t color) override;
    void drawProfileName(const String& name) override;

private:
    TFT_eSPI _tft;

    void resetControllerViaBlkPin();
    void wipeGraphicsRam();
    void configureLandscape();
    void stabilizeBus();
};
