#include "core/ProfileManager.h"
#include "util/Logger.h"

namespace {
constexpr const char* TAG = "PROFILE";
}

ProfileManager::ProfileManager(ConfigManager& config, ProfileRepository& repository, IDisplay& display)
    : _config(config), _repository(repository), _display(display), _currentProfile(0), _profileCount(1) {}

void ProfileManager::begin() {
    countProfiles();
    loadProfile(_currentProfile);
}

void ProfileManager::countProfiles() {
    _profileCount = _repository.countProfiles();

    // Modulo arithmetic in navigation requires at least one profile.
    if (_profileCount == 0) {
        Log::error(TAG, "No profiles found, defaulting count to 1");
        _profileCount = 1;
    } else {
        Log::info(TAG, "Scan complete, found %d profiles", _profileCount);
    }
}

void ProfileManager::nextProfile() {
    _currentProfile = (_currentProfile + 1) % _profileCount;
    loadProfile(_currentProfile);
}

void ProfileManager::previousProfile() {
    _currentProfile = (_currentProfile == 0) ? (_profileCount - 1) : (_currentProfile - 1);
    loadProfile(_currentProfile);
}

void ProfileManager::previewColor(const String& hexColor) {
    _config.setActiveColorHex(hexColor);
    Log::info(TAG, "Live preview color updated to: %s", hexColor.c_str());
}

void ProfileManager::loadProfile(uint8_t index, bool fullClear) {
    Log::info(TAG, "Loading profile_%d (fullClear: %d)", index, fullClear);

    String jsonConfig = _repository.readConfig(index);
    if (jsonConfig.length() == 0 || !_config.loadConfig(jsonConfig)) {
        Log::error(TAG, "Failed to load profile_%d", index);
        return;
    }

    // A full clear is only needed when switching profiles from the device itself.
    if (fullClear) {
        _display.clear();
    }
    _display.drawProfileName(_config.getProfileName());

    IconPosition allPositions[] = {
        IconPosition::LeftUp, IconPosition::LeftLeft, IconPosition::LeftRight, IconPosition::LeftDown,
        IconPosition::RightUp, IconPosition::RightLeft, IconPosition::RightRight, IconPosition::RightDown
    };

    const auto& buttons = _config.getButtons();

    for (IconPosition pos : allPositions) {
        bool iconDrawn = false;

        for (const auto& pair : buttons) {
            if (pair.first == pos && pair.second.iconPath.length() > 0) {
                if (_repository.readIcon(index, pair.second.iconPath, _iconBuffer, ICON_BUFFER_SIZE)) {
                    _display.drawIcon(pos, reinterpret_cast<uint16_t*>(_iconBuffer));
                    iconDrawn = true;
                }
                break;
            }
        }

        if (!iconDrawn && !fullClear) {
            _display.clearIconArea(pos);
        }
    }
    Log::info(TAG, "UI loaded successfully");
}

uint8_t ProfileManager::createNewProfile() {
    uint8_t newId = _profileCount;
    _repository.createProfile(newId);
    _profileCount++;

    Log::info(TAG, "Created profile_%d", newId);
    return newId;
}

bool ProfileManager::deleteProfile(uint8_t index) {
    if (_profileCount <= 1 || index >= _profileCount) {
        Log::error(TAG, "Cannot delete the last profile or out-of-bounds index");
        return false;
    }

    if (!_repository.deleteAndCloseGap(index, _profileCount)) {
        return false;
    }
    _profileCount--;

    // Profile indices have shifted, so the previously active one may no longer exist.
    _currentProfile = 0;
    loadProfile(_currentProfile);
    return true;
}
