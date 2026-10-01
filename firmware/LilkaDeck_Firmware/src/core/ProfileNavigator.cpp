#include "core/ProfileNavigator.h"

uint8_t ProfileNavigator::current() const {
    return _current;
}

uint8_t ProfileNavigator::count() const {
    return _count;
}

uint8_t ProfileNavigator::nextFreeId() const {
    return _count;
}

// Navigation wraps around with modulo arithmetic, which needs at least one profile.
void ProfileNavigator::setProfileCount(uint8_t count) {
    _count = (count == 0) ? 1 : count;
}

uint8_t ProfileNavigator::moveToNext() {
    _current = (_current + 1) % _count;
    return _current;
}

uint8_t ProfileNavigator::moveToPrevious() {
    _current = (_current == 0) ? (_count - 1) : (_current - 1);
    return _current;
}

bool ProfileNavigator::canDelete(uint8_t index) const {
    return _count > 1 && index < _count;
}

void ProfileNavigator::registerCreated() {
    _count++;
}

// Deleting closes the gap in numbering, so the previously active profile may no longer exist.
void ProfileNavigator::registerDeleted() {
    _count--;
    _current = 0;
}
