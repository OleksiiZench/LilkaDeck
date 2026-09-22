#pragma once

#include <Arduino.h>
#include <ArduinoJson.h>
#include <map>
#include <vector>
#include "display/DisplayManager.h"

struct ButtonConfig {
    String iconPath;
    String type;
    std::vector<String> actions;
};

class ConfigManager {
public:
    ConfigManager();

    bool loadConfig(const String& jsonString);
    void setActiveColorHex(const String& hex); // НОВЕ
    const std::map<IconPosition, ButtonConfig>& getButtons() const;
    String getProfileName() const;
    
    // Returns the parsed active color in RGB565 format for the display
    uint16_t getActiveColor() const;

private:
    std::map<IconPosition, ButtonConfig> _buttons;
    String _profileName;
    uint16_t _activeColor;
    
    IconPosition stringToPosition(const String& posStr);
    
    // Utility to convert standard HEX color string (e.g., "#FF8C00") to 16-bit RGB565
    uint16_t hexToRGB565(const String& hex);
};
