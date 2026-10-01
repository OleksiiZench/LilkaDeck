#pragma once

#include <stdint.h>

// Tracks which profile is active and how many exist. Has no hardware dependencies.
class ProfileNavigator {
public:
    uint8_t current() const;
    uint8_t count() const;
    uint8_t nextFreeId() const;

    void setProfileCount(uint8_t count);
    uint8_t moveToNext();
    uint8_t moveToPrevious();

    bool canDelete(uint8_t index) const;
    void registerCreated();
    void registerDeleted();

private:
    uint8_t _current = 0;
    uint8_t _count = 1;
};
