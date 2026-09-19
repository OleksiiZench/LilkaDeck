#include "core/ProfileManager.h"
#include "config/BoardConfig.h"

ProfileManager::ProfileManager(ConfigManager& config, StorageManager& storage, DisplayManager& display)
    : _config(config), _storage(storage), _display(display), _currentProfile(0) {}

void ProfileManager::begin() {
    loadProfile(_currentProfile);
}

void ProfileManager::nextProfile() {
    _currentProfile = (_currentProfile + 1) % MAX_PROFILES;
    loadProfile(_currentProfile);
}

void ProfileManager::previousProfile() {
    _currentProfile = (_currentProfile == 0) ? (MAX_PROFILES - 1) : (_currentProfile - 1);
    loadProfile(_currentProfile);
}

void ProfileManager::loadProfile(uint8_t index) {
    Serial0.printf("[PROFILE] Loading profile_%d...\n", index);
    
    String folderPath = "/profile_" + String(index);
    String configPath = folderPath + "/config.json";

    // Isolate the SPI bus before reading the SD card
    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);
    
    String jsonConfig = _storage.readTextFile(configPath.c_str());
    
    if (jsonConfig.length() > 0 && _config.loadConfig(jsonConfig)) {
        
        digitalWrite(BoardConfig::PIN_SD_CS, HIGH);
        _display.clear();
        
        // Draw the profile name
        _display.drawProfileName(_config.getProfileName());

        for (const auto& pair : _config.getButtons()) {
            IconPosition pos = pair.first;
            // Construct the full path to the icon (for example: /profile_0/icon.rgb565)
            String iconPath = folderPath + "/" + pair.second.iconPath;
            
            if (_storage.readFileToBuffer(iconPath.c_str(), _iconBuffer, ICON_BUFFER_SIZE)) {
                digitalWrite(BoardConfig::PIN_SD_CS, HIGH);
                uint16_t* rgb565Data = reinterpret_cast<uint16_t*>(_iconBuffer);
                _display.drawIcon(pos, rgb565Data);
            }
        }
        Serial0.println("[PROFILE] UI loaded successfully.");
    } else {
        Serial0.printf("[ERR] Failed to load %s\n", configPath.c_str());
    }
}
