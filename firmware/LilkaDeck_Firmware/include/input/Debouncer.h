#pragma once

#include <stdint.h>

// Filters contact bounce: a new state is accepted only after the raw reading
// has stayed unchanged for longer than the debounce delay.
class Debouncer {
public:
    // Returns true when the stable state has just changed.
    bool update(bool pressed, uint32_t nowMs);
    bool isPressed() const;

private:
    bool _lastReading = false;
    bool _stableState = false;
    uint32_t _lastChangeMs = 0;
};
