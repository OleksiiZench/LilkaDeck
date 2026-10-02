#include "input/ButtonLayout.h"

bool tryGetIconPosition(ButtonId button, IconPosition& outPosition) {
    switch (button) {
        case ButtonId::Up:     outPosition = IconPosition::LeftUp;     return true;
        case ButtonId::Down:   outPosition = IconPosition::LeftDown;   return true;
        case ButtonId::Left:   outPosition = IconPosition::LeftLeft;   return true;
        case ButtonId::Right:  outPosition = IconPosition::LeftRight;  return true;

        case ButtonId::C:      outPosition = IconPosition::RightUp;    return true;
        case ButtonId::B:      outPosition = IconPosition::RightDown;  return true;
        case ButtonId::D:      outPosition = IconPosition::RightLeft;  return true;
        case ButtonId::A:      outPosition = IconPosition::RightRight; return true;

        case ButtonId::Select:
        case ButtonId::Start:
        case ButtonId::Count:
            return false;
    }
    return false;
}
