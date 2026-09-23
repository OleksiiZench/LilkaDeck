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

void ProfileManager::loadProfile(uint8_t index, bool fullClear) {
    Serial.printf("[PROFILE] Loading profile_%d (fullClear: %d)...\n", index, fullClear);
    
    String folderPath = "/profile_" + String(index);
    String configPath = folderPath + "/config.json";

    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);
    
    String jsonConfig = _storage.readTextFile(configPath.c_str());
    
    if (jsonConfig.length() > 0 && _config.loadConfig(jsonConfig)) {
        digitalWrite(BoardConfig::PIN_SD_CS, HIGH);
        
        // Clear the entire screen ONLY when switching profiles using the buttons on Lilka itself
        if (fullClear) {
            _display.clear();
        }
        
        // Draws text (you've already implemented this safely using `fillRect`; it doesn't flicker)
        _display.drawProfileName(_config.getProfileName());

        // Array of all possible buttons on the macro pad
        IconPosition allPositions[] = {
            IconPosition::LeftUp, IconPosition::LeftLeft, IconPosition::LeftRight, IconPosition::LeftDown,
            IconPosition::RightUp, IconPosition::RightLeft, IconPosition::RightRight, IconPosition::RightDown
        };

        const auto& buttons = _config.getButtons();

        for (IconPosition pos : allPositions) {
            bool iconDrawn = false;
            
            for (const auto& pair : buttons) {
                if (pair.first == pos && pair.second.iconPath.length() > 0) {
                    String iconPath = folderPath + "/" + pair.second.iconPath;
                    if (_storage.readFileToBuffer(iconPath.c_str(), _iconBuffer, ICON_BUFFER_SIZE)) {
                        digitalWrite(BoardConfig::PIN_SD_CS, HIGH);
                        uint16_t* rgb565Data = reinterpret_cast<uint16_t*>(_iconBuffer);
                        _display.drawIcon(pos, rgb565Data);
                        iconDrawn = true;
                    }
                    break;
                }
            }
            
            if (!iconDrawn && !fullClear) {
                _display.clearIconArea(pos);
            }
        }
        Serial.println("[PROFILE] UI loaded successfully.");
    } else {
        Serial.printf("[ERR] Failed to load %s\n", configPath.c_str());
    }
}

uint8_t ProfileManager::createNewProfile() {
    uint8_t newId = _profileCount;
    String dirPath = "/profile_" + String(newId);
    String configPath = dirPath + "/config.json";

    digitalWrite(BoardConfig::PIN_SD_CS, HIGH); // Isolate SPI bus

    // Create the new folder
    _storage.createDir(dirPath.c_str());

    // Generate a default valid config.json
    String defaultConfig = "{\n  \"profileName\": \"New Profile\",\n  \"activeColor\": \"#00FFFF\",\n  \"buttons\": {}\n}";
    _storage.writeTextFile(configPath.c_str(), defaultConfig.c_str());

    digitalWrite(BoardConfig::PIN_SD_CS, HIGH); // Isolate SPI bus

    _profileCount++;
    Serial.printf("[PROFILE] Created new profile_%d\n", newId);
    
    return newId;
}

bool ProfileManager::deleteProfile(uint8_t index) {
    // Prevent deleting the last remaining profile or non-existent profiles
    if (_profileCount <= 1 || index >= _profileCount) {
        Serial.println("[ERR] Cannot delete the last profile or out-of-bounds index.");
        return false;
    }

    String dirPath = "/profile_" + String(index);

    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);

    // 1. Delete the targeted folder and all files inside it
    _storage.deleteDirRecursive(dirPath.c_str());
    Serial.printf("[PROFILE] Deleted %s\n", dirPath.c_str());

    // 2. Shift all subsequent profiles down by 1 to close the gap
    for (uint8_t i = index + 1; i < _profileCount; i++) {
        String oldPath = "/profile_" + String(i);
        String newPath = "/profile_" + String(i - 1);
        _storage.renameFileOrDir(oldPath.c_str(), newPath.c_str());
        Serial.printf("[PROFILE] Renamed %s to %s\n", oldPath.c_str(), newPath.c_str());
    }

    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);

    _profileCount--;

    // 3. Ensure UI safety by forcing navigation to profile_0 after a deletion
    _currentProfile = 0;
    loadProfile(_currentProfile);
    
    return true;
}
