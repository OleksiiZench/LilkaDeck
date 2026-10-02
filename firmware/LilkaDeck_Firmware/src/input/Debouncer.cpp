#include "input/Debouncer.h"

namespace {
constexpr uint32_t DEBOUNCE_DELAY_MS = 20;
}

bool Debouncer::update(bool pressed, uint32_t nowMs) {
    if (pressed != _lastReading) {
        _lastChangeMs = nowMs;
    }
    _lastReading = pressed;

    bool isStable = (nowMs - _lastChangeMs) > DEBOUNCE_DELAY_MS;
    if (!isStable || pressed == _stableState) return false;

    _stableState = pressed;
    return true;
}

bool Debouncer::isPressed() const {
    return _stableState;
}
