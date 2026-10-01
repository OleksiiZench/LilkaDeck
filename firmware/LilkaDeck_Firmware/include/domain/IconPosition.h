#pragma once

enum class IconPosition {
    LeftUp, LeftLeft, LeftRight, LeftDown,
    RightUp, RightLeft, RightRight, RightDown
};

constexpr IconPosition ALL_ICON_POSITIONS[] = {
    IconPosition::LeftUp, IconPosition::LeftLeft, IconPosition::LeftRight, IconPosition::LeftDown,
    IconPosition::RightUp, IconPosition::RightLeft, IconPosition::RightRight, IconPosition::RightDown
};

bool tryParseIconPosition(const char* name, IconPosition& outPosition);
