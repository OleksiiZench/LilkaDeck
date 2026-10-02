#include "config/ConfigManager.h"
#include <ArduinoJson.h>
#include "domain/ColorUtils.h"
#include "util/Logger.h"

namespace {

constexpr const char* TAG = "CONFIG";
constexpr const char* DEFAULT_PROFILE_NAME = "Profile";
constexpr const char* DEFAULT_ACTIVE_COLOR = "#00FFFF";
constexpr const char* DEFAULT_BUTTON_TYPE = "shortcut";

ButtonConfig parseButton(JsonVariantConst data) {
    ButtonConfig button;
    button.iconPath = data["icon"].as<String>();
    button.type = data["type"] | DEFAULT_BUTTON_TYPE;

    for (JsonVariantConst action : data["action"].as<JsonArrayConst>()) {
        button.actions.push_back(action.as<String>());
    }
    return button;
}

std::map<IconPosition, ButtonConfig> parseButtons(JsonObjectConst buttons) {
    std::map<IconPosition, ButtonConfig> parsed;

    for (JsonPairConst entry : buttons) {
        IconPosition position;
        if (!tryParseIconPosition(entry.key().c_str(), position)) {
            Log::error(TAG, "Unknown button position: %s", entry.key().c_str());
            continue;
        }
        parsed[position] = parseButton(entry.value());
    }
    return parsed;
}

}

ConfigManager::ConfigManager() : _activeColor(Color::Cyan) {}

bool ConfigManager::loadConfig(const String& jsonString) {
    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, jsonString);
    if (error) {
        Log::error(TAG, "JSON parse failed: %s", error.c_str());
        return false;
    }

    _profileName = doc["profileName"] | DEFAULT_PROFILE_NAME;
    setActiveColorHex(doc["activeColor"] | DEFAULT_ACTIVE_COLOR);
    _buttons = parseButtons(doc["buttons"].as<JsonObjectConst>());
    return true;
}

void ConfigManager::setActiveColorHex(const String& hex) {
    _activeColor = Color::fromHex(hex.c_str());
}

const std::map<IconPosition, ButtonConfig>& ConfigManager::getButtons() const {
    return _buttons;
}

String ConfigManager::getProfileName() const {
    return _profileName;
}

uint16_t ConfigManager::getActiveColor() const {
    return _activeColor;
}
