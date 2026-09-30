#pragma once

#include <stdint.h>
#include "domain/IconPosition.h"

struct ScreenPoint {
    int32_t x;
    int32_t y;
};

namespace IconGridLayout {

constexpr int32_t ICON_SIZE = 64;

ScreenPoint originOf(IconPosition position);

}
