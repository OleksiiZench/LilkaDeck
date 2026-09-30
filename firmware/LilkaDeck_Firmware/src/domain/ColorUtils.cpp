#include "domain/ColorUtils.h"
#include <stdlib.h>
#include <string.h>

namespace {

constexpr size_t HEX_DIGIT_COUNT = 6;

// Compresses 8-8-8 RGB into the display's native 5-6-5 format.
uint16_t packRgb565(uint8_t red, uint8_t green, uint8_t blue) {
    return ((red & 0xF8) << 8) | ((green & 0xFC) << 3) | (blue >> 3);
}

const char* skipLeadingHash(const char* hex) {
    return hex[0] == '#' ? hex + 1 : hex;
}

}

uint16_t Color::fromHex(const char* hex, uint16_t fallback) {
    if (hex == nullptr) return fallback;

    const char* digits = skipLeadingHash(hex);
    if (strlen(digits) != HEX_DIGIT_COUNT) return fallback;

    char* parseEnd = nullptr;
    long rgb = strtol(digits, &parseEnd, 16);
    if (*parseEnd != '\0') return fallback;

    return packRgb565((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
}
