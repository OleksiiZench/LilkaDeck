#include "core/ProfileManager.h"
#include "util/Logger.h"

namespace {
constexpr const char* TAG = "PROFILE";
}

ProfileManager::ProfileManager(ConfigManager& config, ProfileRepository& repository,
                               ProfileNavigator& navigator, ProfilePresenter& presenter)
    : _config(config), _repository(repository), _navigator(navigator), _presenter(presenter) {}

void ProfileManager::begin() {
    discoverProfiles();
    _presenter.show(_navigator.current(), RedrawMode::Full);
}

void ProfileManager::discoverProfiles() {
    uint8_t count = _repository.countProfiles();

    if (count == 0) {
        Log::error(TAG, "No profiles found, defaulting count to 1");
    } else {
        Log::info(TAG, "Scan complete, found %d profiles", count);
    }
    _navigator.setProfileCount(count);
}

void ProfileManager::nextProfile() {
    _presenter.show(_navigator.moveToNext(), RedrawMode::Full);
}

void ProfileManager::previousProfile() {
    _presenter.show(_navigator.moveToPrevious(), RedrawMode::Full);
}

void ProfileManager::reloadProfile(uint8_t index) {
    _presenter.show(index, RedrawMode::Incremental);
}

void ProfileManager::previewColor(const String& hexColor) {
    _config.setActiveColorHex(hexColor);
    Log::info(TAG, "Live preview color updated to: %s", hexColor.c_str());
}

uint8_t ProfileManager::createNewProfile() {
    uint8_t newId = _navigator.nextFreeId();
    _repository.createProfile(newId);
    _navigator.registerCreated();

    Log::info(TAG, "Created profile_%d", newId);
    return newId;
}

bool ProfileManager::deleteProfile(uint8_t index) {
    if (!_navigator.canDelete(index)) {
        Log::error(TAG, "Cannot delete the last profile or out-of-bounds index");
        return false;
    }

    if (!_repository.deleteAndCloseGap(index, _navigator.count())) {
        return false;
    }
    _navigator.registerDeleted();

    _presenter.show(_navigator.current(), RedrawMode::Full);
    return true;
}
