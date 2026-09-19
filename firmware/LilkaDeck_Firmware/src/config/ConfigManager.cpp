#include "config/ConfigManager.h"

ConfigManager::ConfigManager() {}

bool ConfigManager::loadConfig(const String& jsonString) {
    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, jsonString);

    if (error) {
        Serial0.print("[CONFIG] JSON Parse Error: ");
        Serial0.println(error.c_str());
        return false;
    }

    _buttons.clear();

    _profileName = doc["profileName"] | "Profile";

    JsonObject buttons = doc["buttons"];
    for (JsonPair kv : buttons) {
        String posStr = kv.key().c_str();
        JsonObject btnData = kv.value();

        ButtonConfig config;
        config.iconPath = btnData["icon"].as<String>();
        
        JsonArray actions = btnData["action"];
        for (JsonVariant v : actions) {
            config.actions.push_back(v.as<String>());
        }

        IconPosition pos = stringToPosition(posStr);
        _buttons[pos] = config;
    }
    
    return true;
}

const std::map<IconPosition, ButtonConfig>& ConfigManager::getButtons() const {
    return _buttons;
}

String ConfigManager::getProfileName() const
{
    return _profileName;
}

IconPosition ConfigManager::stringToPosition(const String& posStr) {
    if (posStr == "LeftUp") return IconPosition::LeftUp;
    if (posStr == "LeftLeft") return IconPosition::LeftLeft;
    if (posStr == "LeftRight") return IconPosition::LeftRight;
    if (posStr == "LeftDown") return IconPosition::LeftDown;
    if (posStr == "RightUp") return IconPosition::RightUp;
    if (posStr == "RightLeft") return IconPosition::RightLeft;
    if (posStr == "RightRight") return IconPosition::RightRight;
    if (posStr == "RightDown") return IconPosition::RightDown;
    return IconPosition::LeftUp; // Fallback
}
