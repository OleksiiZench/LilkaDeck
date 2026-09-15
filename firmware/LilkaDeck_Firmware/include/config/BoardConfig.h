#pragma once
#include <stdint.h>

namespace BoardConfig {
    // Hardware specific pins not managed by TFT_eSPI build_flags
    constexpr uint8_t PIN_POWER_ENABLE = 46;
    constexpr uint8_t PIN_SD_CS = 16;
}
