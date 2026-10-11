#include "input/UsbHidOutput.h"

UsbHidOutput::UsbHidOutput(USBHIDKeyboard& keyboard) : _keyboard(keyboard) {}

void UsbHidOutput::begin() {
    _mediaControl.begin();
}

void UsbHidOutput::pressKey(uint8_t keycode) {
    // Modifier keycodes 0x80-0x87 are HID usages 0xE0-0xE7. The library's press() only sets the
    // modifier bits in its internal report and then calls pressRaw(0), which does not send it, so a
    // lone modifier (for example the Win/Super key) would do nothing. pressRaw() sends the report.
    if (keycode >= 0x80 && keycode < 0x88) {
        _keyboard.pressRaw(0xE0 + (keycode - 0x80));
        return;
    }
    _keyboard.press(keycode);
}

void UsbHidOutput::tapMediaKey(uint16_t usageCode) {
    _mediaControl.press(usageCode);
    _mediaControl.release();
}

void UsbHidOutput::releaseAllKeys() {
    _keyboard.releaseAll();
}
