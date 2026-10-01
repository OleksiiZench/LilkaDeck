#pragma once

#include <Arduino.h>
#include "config/ConfigManager.h"
#include "display/IDisplay.h"
#include "display/IconGridLayout.h"
#include "storage/ProfileRepository.h"

enum class RedrawMode {
    // Clears the whole screen first; used when switching profiles.
    Full,
    // Redraws in place without flicker; used when the same profile was updated.
    Incremental
};

// Loads a profile from storage and renders it on the display.
class ProfilePresenter {
public:
    ProfilePresenter(ConfigManager& config, ProfileRepository& repository, IDisplay& display);

    bool show(uint8_t profileId, RedrawMode mode);

private:
    static constexpr size_t ICON_BUFFER_SIZE =
        IconGridLayout::ICON_SIZE * IconGridLayout::ICON_SIZE * sizeof(uint16_t);

    ConfigManager& _config;
    ProfileRepository& _repository;
    IDisplay& _display;
    alignas(4) uint8_t _iconBuffer[ICON_BUFFER_SIZE];

    bool loadConfig(uint8_t profileId);
    void drawIcons(uint8_t profileId, RedrawMode mode);
    bool tryDrawIcon(uint8_t profileId, IconPosition position);
};
