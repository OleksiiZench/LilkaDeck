#pragma once

#include <Arduino.h>
#include "domain/IconPosition.h"

class IDisplay {
public:
    virtual ~IDisplay() = default;

    virtual void clear() = 0;
    virtual void drawIcon(IconPosition position, uint16_t* imageBuffer) = 0;
    virtual void clearIconArea(IconPosition position) = 0;
    virtual void setIconPressed(IconPosition position, bool isPressed, uint16_t color) = 0;
    virtual void drawProfileName(const String& name) = 0;
};
