#include "domain/IconPosition.h"
#include <cstring>

namespace {

struct PositionName {
    const char* name;
    IconPosition position;
};

constexpr PositionName POSITION_NAMES[] = {
    {"LeftUp", IconPosition::LeftUp},
    {"LeftLeft", IconPosition::LeftLeft},
    {"LeftRight", IconPosition::LeftRight},
    {"LeftDown", IconPosition::LeftDown},
    {"RightUp", IconPosition::RightUp},
    {"RightLeft", IconPosition::RightLeft},
    {"RightRight", IconPosition::RightRight},
    {"RightDown", IconPosition::RightDown},
};

}

bool tryParseIconPosition(const char* name, IconPosition& outPosition) {
    for (const PositionName& entry : POSITION_NAMES) {
        if (strcmp(entry.name, name) == 0) {
            outPosition = entry.position;
            return true;
        }
    }
    return false;
}
