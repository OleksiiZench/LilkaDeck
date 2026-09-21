#pragma once
#include <Arduino.h>
#include "config/ConfigManager.h"
#include "storage/StorageManager.h"
#include "display/DisplayManager.h"

class ProfileManager {
public:
    ProfileManager(ConfigManager& config, StorageManager& storage, DisplayManager& display);
    
    void begin();
    void loadProfile(uint8_t index);
    void nextProfile();
    void previousProfile();

private:
    ConfigManager& _config;
    StorageManager& _storage;
    DisplayManager& _display;
    
    uint8_t _currentProfile;
    static const uint8_t MAX_PROFILES = 4;
    
    static const size_t ICON_BUFFER_SIZE = 64 * 64 * 2;
    uint8_t _iconBuffer[ICON_BUFFER_SIZE] __attribute__((aligned(4)));
};
