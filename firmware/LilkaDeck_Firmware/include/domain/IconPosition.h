#pragma once

enum class IconPosition {
    LeftUp, LeftLeft, LeftRight, LeftDown,
    RightUp, RightLeft, RightRight, RightDown
};

bool tryParseIconPosition(const char* name, IconPosition& outPosition);
