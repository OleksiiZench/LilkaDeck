#pragma once

#include <stddef.h>

enum class ButtonId {
    Up, Down, Left, Right,
    A, B, C, D,
    Select, Start,
    Count
};

constexpr size_t BUTTON_COUNT = static_cast<size_t>(ButtonId::Count);
