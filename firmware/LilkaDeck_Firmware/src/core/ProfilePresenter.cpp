#include "core/ProfilePresenter.h"
#include "util/Logger.h"

namespace {
constexpr const char* TAG = "PROFILE";
}

ProfilePresenter::ProfilePresenter(ConfigManager& config, ProfileRepository& repository, IDisplay& display)
    : _config(config), _repository(repository), _display(display) {}

bool ProfilePresenter::show(uint8_t profileId, RedrawMode mode) {
    Log::info(TAG, "Loading profile_%d (%s redraw)", profileId, mode == RedrawMode::Full ? "full" : "incremental");

    if (!loadConfig(profileId)) {
        Log::error(TAG, "Failed to load profile_%d", profileId);
        return false;
    }

    if (mode == RedrawMode::Full) {
        _display.clear();
    }
    _display.drawProfileName(_config.getProfileName());
    drawIcons(profileId, mode);

    Log::info(TAG, "UI loaded successfully");
    return true;
}

bool ProfilePresenter::loadConfig(uint8_t profileId) {
    String json = _repository.readConfig(profileId);
    return json.length() > 0 && _config.loadConfig(json);
}

void ProfilePresenter::drawIcons(uint8_t profileId, RedrawMode mode) {
    for (IconPosition position : ALL_ICON_POSITIONS) {
        bool drawn = tryDrawIcon(profileId, position);
        if (!drawn && mode == RedrawMode::Incremental) {
            _display.clearIconArea(position);
        }
    }
}

bool ProfilePresenter::tryDrawIcon(uint8_t profileId, IconPosition position) {
    const auto& buttons = _config.getButtons();
    auto button = buttons.find(position);
    if (button == buttons.end() || button->second.iconPath.length() == 0) return false;

    if (!_repository.readIcon(profileId, button->second.iconPath, _iconBuffer, sizeof(_iconBuffer))) {
        return false;
    }
    _display.drawIcon(position, reinterpret_cast<uint16_t*>(_iconBuffer));
    return true;
}
