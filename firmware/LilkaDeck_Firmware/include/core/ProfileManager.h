#pragma once

#include <Arduino.h>
#include "config/ConfigManager.h"
#include "core/IProfileSwitcher.h"
#include "core/ProfileNavigator.h"
#include "core/ProfilePresenter.h"
#include "storage/ProfileRepository.h"

// Coordinates profile use cases: startup, switching, creating, deleting and live color preview.
class ProfileManager : public IProfileSwitcher {
public:
    ProfileManager(ConfigManager& config, ProfileRepository& repository,
                   ProfileNavigator& navigator, ProfilePresenter& presenter);

    void begin();
    void nextProfile() override;
    void previousProfile() override;
    void reloadProfile(uint8_t index);
    void previewColor(const String& hexColor);

    uint8_t createNewProfile();
    bool deleteProfile(uint8_t index);

private:
    ConfigManager& _config;
    ProfileRepository& _repository;
    ProfileNavigator& _navigator;
    ProfilePresenter& _presenter;

    void discoverProfiles();
};
