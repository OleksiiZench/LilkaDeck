#pragma once

#include <stdint.h>

// Keycodes missing from USBHIDKeyboard.h.
// The library's press() subtracts 0x88 to get the raw USB HID Usage ID,
// so each constant equals 0x88 + <standard HID Usage Table code>.
namespace ExtendedKey {

constexpr uint8_t NumLock = 0xDB;
constexpr uint8_t ScrollLock = 0xCF;
constexpr uint8_t PrintScreen = 0xCE;
constexpr uint8_t Pause = 0xD0;
constexpr uint8_t Menu = 0xED;

constexpr uint8_t KeypadSlash = 0xDC;
constexpr uint8_t KeypadAsterisk = 0xDD;
constexpr uint8_t KeypadMinus = 0xDE;
constexpr uint8_t KeypadPlus = 0xDF;
constexpr uint8_t KeypadEnter = 0xE0;
constexpr uint8_t Keypad1 = 0xE1;
constexpr uint8_t Keypad2 = 0xE2;
constexpr uint8_t Keypad3 = 0xE3;
constexpr uint8_t Keypad4 = 0xE4;
constexpr uint8_t Keypad5 = 0xE5;
constexpr uint8_t Keypad6 = 0xE6;
constexpr uint8_t Keypad7 = 0xE7;
constexpr uint8_t Keypad8 = 0xE8;
constexpr uint8_t Keypad9 = 0xE9;
constexpr uint8_t Keypad0 = 0xEA;
constexpr uint8_t KeypadDot = 0xEB;

}
