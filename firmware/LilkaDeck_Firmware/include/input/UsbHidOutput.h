#pragma once

#include <USBHIDConsumerControl.h>
#include <USBHIDKeyboard.h>
#include "input/IHidOutput.h"

class UsbHidOutput : public IHidOutput {
public:
    explicit UsbHidOutput(USBHIDKeyboard& keyboard);

    // Registers the consumer-control endpoint; call after USB.begin().
    void begin();

    void pressKey(uint8_t keycode) override;
    void tapMediaKey(uint16_t usageCode) override;
    void releaseAllKeys() override;

private:
    USBHIDKeyboard& _keyboard;
    USBHIDConsumerControl _mediaControl;
};
