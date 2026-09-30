#include "display/IconGridLayout.h"

namespace {

constexpr int32_t ROW_UP = 18;
constexpr int32_t ROW_MIDDLE = 88;
constexpr int32_t ROW_DOWN = 158;

constexpr int32_t LEFT_CLUSTER_LEFT_X = 4;
constexpr int32_t LEFT_CLUSTER_CENTER_X = 37;
constexpr int32_t LEFT_CLUSTER_RIGHT_X = 70;

constexpr int32_t RIGHT_CLUSTER_LEFT_X = 146;
constexpr int32_t RIGHT_CLUSTER_CENTER_X = 179;
constexpr int32_t RIGHT_CLUSTER_RIGHT_X = 212;

}

ScreenPoint IconGridLayout::originOf(IconPosition position) {
    switch (position) {
        case IconPosition::LeftUp:     return {LEFT_CLUSTER_CENTER_X, ROW_UP};
        case IconPosition::LeftLeft:   return {LEFT_CLUSTER_LEFT_X, ROW_MIDDLE};
        case IconPosition::LeftRight:  return {LEFT_CLUSTER_RIGHT_X, ROW_MIDDLE};
        case IconPosition::LeftDown:   return {LEFT_CLUSTER_CENTER_X, ROW_DOWN};

        case IconPosition::RightUp:    return {RIGHT_CLUSTER_CENTER_X, ROW_UP};
        case IconPosition::RightLeft:  return {RIGHT_CLUSTER_LEFT_X, ROW_MIDDLE};
        case IconPosition::RightRight: return {RIGHT_CLUSTER_RIGHT_X, ROW_MIDDLE};
        case IconPosition::RightDown:  return {RIGHT_CLUSTER_CENTER_X, ROW_DOWN};
    }
    return {0, 0};
}
