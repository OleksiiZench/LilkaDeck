#pragma once

#include <stdint.h>

namespace BoardConfig {

// Pins not managed by TFT_eSPI build_flags.

// Driven LOW at the very start of boot to reset the display controller;
// without it the panel shows noise on power-up.
constexpr uint8_t PIN_DISPLAY_BLK = 46;
constexpr uint8_t PIN_SD_CS = 16;

}