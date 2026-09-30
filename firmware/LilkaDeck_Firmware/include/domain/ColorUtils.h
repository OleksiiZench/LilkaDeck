#pragma once

#include <stdint.h>

namespace Color {

constexpr uint16_t Cyan = 0x07FF;

// Accepts "#RRGGBB" or "RRGGBB". Returns fallback for malformed input.
uint16_t fromHex(const char* hex, uint16_t fallback = Cyan);

}
