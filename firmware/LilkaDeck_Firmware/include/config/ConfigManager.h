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
    const std::map<IconPosition, ButtonConfig>& getButtons() const;
    String getProfileName() const;

private:
    std::map<IconPosition, ButtonConfig> _buttons;
    String _profileName;
    
    IconPosition stringToPosition(const String& posStr);
};
