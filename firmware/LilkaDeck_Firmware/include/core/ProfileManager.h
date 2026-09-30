#pragma once
#include <Arduino.h>
#include "config/ConfigManager.h"
#include "storage/ProfileRepository.h"
#include "display/DisplayManager.h"

class ProfileManager {
public:
    ProfileManager(ConfigManager& config, ProfileRepository& repository, DisplayManager& display);
    void begin();
    void loadProfile(uint8_t index, bool fullClear = true);
    void nextProfile();
    void previousProfile();
    void previewColor(const String& hexColor);

    uint8_t createNewProfile();
    bool deleteProfile(uint8_t index);

private:
    ConfigManager& _config;
    ProfileRepository& _repository;
    DisplayManager& _display;
    uint8_t _currentProfile;
    uint8_t _profileCount;
    static const size_t ICON_BUFFER_SIZE = 64 * 64 * 2;
    uint8_t _iconBuffer[ICON_BUFFER_SIZE] __attribute__((aligned(4)));

    void countProfiles();
};
