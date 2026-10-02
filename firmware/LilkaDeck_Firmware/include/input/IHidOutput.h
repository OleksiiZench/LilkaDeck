#pragma once

#include <stdint.h>

class IHidOutput {
public:
    virtual ~IHidOutput() = default;

    // Keeps the key held until releaseAllKeys() is called.
    virtual void pressKey(uint8_t keycode) = 0;
    // Sends a complete press-and-release of a media key.
    virtual void tapMediaKey(uint16_t usageCode) = 0;
    virtual void releaseAllKeys() = 0;
};
