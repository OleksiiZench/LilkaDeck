#pragma once

#include <stdint.h>

namespace BoardConfig {

// Pins not managed by TFT_eSPI build_flags.

// Driven LOW at the very start of boot to reset the display controller;
// without it the panel shows noise on power-up.
constexpr uint8_t PIN_DISPLAY_BLK = 46;
constexpr uint8_t PIN_SD_CS = 16;

// Buttons are wired to GND and read with internal pull-ups, so pressed means LOW.
constexpr uint8_t PIN_BUTTON_UP = 38;
constexpr uint8_t PIN_BUTTON_DOWN = 41;
constexpr uint8_t PIN_BUTTON_LEFT = 39;
constexpr uint8_t PIN_BUTTON_RIGHT = 40;
constexpr uint8_t PIN_BUTTON_A = 5;
constexpr uint8_t PIN_BUTTON_B = 6;
constexpr uint8_t PIN_BUTTON_C = 10;
constexpr uint8_t PIN_BUTTON_D = 9;
// Select (GPIO 0) is also the strap pin that enters flashing mode.
constexpr uint8_t PIN_BUTTON_SELECT = 0;
constexpr uint8_t PIN_BUTTON_START = 4;

}
