#include "input/UsbHidOutput.h"

UsbHidOutput::UsbHidOutput(USBHIDKeyboard& keyboard) : _keyboard(keyboard) {}

void UsbHidOutput::begin() {
    _mediaControl.begin();
}

void UsbHidOutput::pressKey(uint8_t keycode) {
    _keyboard.press(keycode);
}

void UsbHidOutput::tapMediaKey(uint16_t usageCode) {
    _mediaControl.press(usageCode);
    _mediaControl.release();
}

void UsbHidOutput::releaseAllKeys() {
    _keyboard.releaseAll();
}
