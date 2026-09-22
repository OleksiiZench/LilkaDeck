#include "config/ConfigManager.h"

ConfigManager::ConfigManager() : _activeColor(0x07FF) {} // Default to Cyan if not set

bool ConfigManager::loadConfig(const String& jsonString) {
    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, jsonString);

    if (error) {
        Serial.print("[CONFIG] JSON Parse Error: ");
        Serial.println(error.c_str());
        return false;
    }

    _buttons.clear();

    _profileName = doc["profileName"] | "Profile";
    
    // Parse the global active color from JSON. Default to Cyan (#00FFFF) if missing.
    String colorHex = doc["activeColor"] | "#00FFFF";
    _activeColor = hexToRGB565(colorHex);

    JsonObject buttons = doc["buttons"];
    for (JsonPair kv : buttons) {
        String posStr = kv.key().c_str();
        JsonObject btnData = kv.value();

        ButtonConfig config;
        config.iconPath = btnData["icon"].as<String>();

        config.type = btnData["type"] | "shortcut";
        
        JsonArray actions = btnData["action"];
        for (JsonVariant v : actions) {
            config.actions.push_back(v.as<String>());
        }

        IconPosition pos = stringToPosition(posStr);
        _buttons[pos] = config;
    }
    
    return true;
}

void ConfigManager::setActiveColorHex(const String& hex) {
    _activeColor = hexToRGB565(hex);
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

uint16_t ConfigManager::hexToRGB565(const String& hex) {
    String cleanHex = hex;
    
    // Remove the leading hash if it exists
    if (cleanHex.startsWith("#")) {
        cleanHex = cleanHex.substring(1);
    }
    
    // Fallback to Cyan if the hex string is invalid
    if (cleanHex.length() != 6) {
        return 0x07FF; 
    }

    // Parse the 24-bit RGB integer from the hex string
    long rgb = strtol(cleanHex.c_str(), nullptr, 16);
    
    // Extract individual 8-bit color channels
    uint8_t r = (rgb >> 16) & 0xFF;
    uint8_t g = (rgb >> 8) & 0xFF;
    uint8_t b = rgb & 0xFF;

    // Bit-shift to compress 24-bit RGB (8-8-8) down to 16-bit RGB565 (5-6-5)
    return ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3);
}
