#include "core/ProfileManager.h"
#include "config/BoardConfig.h"

ProfileManager::ProfileManager(ConfigManager& config, StorageManager& storage, DisplayManager& display)
    : _config(config), _storage(storage), _display(display), _currentProfile(0), _profileCount(1) {}

void ProfileManager::begin() {
    countProfiles(); // Dynamically count available profiles before loading
    loadProfile(_currentProfile);
}

// Scans the SD card sequentially (profile_0, profile_1, ...) to determine the total count
void ProfileManager::countProfiles() {
    _profileCount = 0;
    
    while (true) {
        String configPath = "/profile_" + String(_profileCount) + "/config.json";
        
        // Isolate the SPI bus before interacting with the SD card
        digitalWrite(BoardConfig::PIN_SD_CS, HIGH);
        
        // Try to read the config. If it returns content, the profile exists.
        String jsonConfig = _storage.readTextFile(configPath.c_str());
        
        if (jsonConfig.length() > 0) {
            _profileCount++;
        } else {
            // Stop scanning as soon as a sequential profile folder is missing
            break; 
        }
    }
    
    // Safety Fallback: Prevent divide-by-zero in modulo operations if SD is empty/corrupt
    if (_profileCount == 0) {
        Serial.println("[PROFILE] Warning: No profiles found! Defaulting count to 1.");
        _profileCount = 1;
    } else {
        Serial.printf("[PROFILE] Scan complete. Found %d active profiles.\n", _profileCount);
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
    Serial.printf("[PROFILE] Live Preview Color updated to: %s\n", hexColor.c_str());
}

void ProfileManager::loadProfile(uint8_t index) {
    Serial.printf("[PROFILE] Loading profile_%d...\n", index);
    
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
        Serial.println("[PROFILE] UI loaded successfully.");
    } else {
        Serial.printf("[ERR] Failed to load %s\n", configPath.c_str());
    }
}
