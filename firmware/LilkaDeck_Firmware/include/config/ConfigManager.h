#pragma once

#include <Arduino.h>
#include <map>
#include <vector>
#include "domain/IconPosition.h"

struct ButtonConfig {
    String iconPath;
    String type;
    std::vector<String> actions;
};

class ConfigManager {
public:
    ConfigManager();

    bool loadConfig(const String& jsonString);
    void setActiveColorHex(const String& hex);

    const std::map<IconPosition, ButtonConfig>& getButtons() const;
    String getProfileName() const;
    uint16_t getActiveColor() const;

private:
    std::map<IconPosition, ButtonConfig> _buttons;
    String _profileName;
    uint16_t _activeColor;
};
