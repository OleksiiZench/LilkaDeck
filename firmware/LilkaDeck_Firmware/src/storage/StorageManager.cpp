#include "storage/StorageManager.h"
#include <Arduino.h>

StorageManager::StorageManager() : _isMounted(false) {}

bool StorageManager::begin(SPIClass& spiBus, uint8_t csPin) {
    if (!SD.begin(csPin, spiBus)) {
        Serial.println("[ERROR] StorageManager: SD Mount Failed!");
        return false;
    }
    
    uint8_t cardType = SD.cardType();
    if (cardType == CARD_NONE) {
        Serial.println("[ERROR] StorageManager: No SD card attached.");
        return false;
    }

    _isMounted = true;
    Serial.printf("[SYS] StorageManager: SD Card Initialized. Type: %d\n", cardType);
    
    return true;
}

bool StorageManager::readFileToBuffer(const char* path, uint8_t* buffer, size_t bufferSize) {
    if (!_isMounted) return false;

    File file = SD.open(path, FILE_READ);
    if (!file) return false;

    size_t fileSize = file.size();
    if (fileSize > bufferSize) fileSize = bufferSize;

    size_t bytesRead = file.read(buffer, fileSize);
    file.close();

    return (bytesRead == fileSize);
}

String StorageManager::readTextFile(const char* path) {
    File file = SD.open(path);
    if (!file) return "";
    String content = file.readString();
    file.close();
    return content;
}

bool StorageManager::createDir(const char* path) {
    if (!_isMounted) return false;
    if (SD.exists(path)) return true;
    return SD.mkdir(path);
}

bool StorageManager::openFileForWrite(const char* path) {
    if (!_isMounted) return false;
    
    if (_activeWriteFile) {
        _activeWriteFile.close();
    }
    
    // Overwrite safely
    if (SD.exists(path)) {
        SD.remove(path);
    }
    
    _activeWriteFile = SD.open(path, FILE_WRITE);
    return _activeWriteFile == true;
}

bool StorageManager::writeChunk(const uint8_t* data, size_t len) {
    if (!_activeWriteFile) return false;
    return _activeWriteFile.write(data, len) == len;
}

void StorageManager::closeFile() {
    if (_activeWriteFile) {
        _activeWriteFile.close();
    }
}

String StorageManager::getProfilesList() {
    String profiles = "";
    File root = SD.open("/");
    if (!root) return profiles;

    File file = root.openNextFile();
    while (file) {
        if (file.isDirectory()) {
            String name = String(file.name());
            if (name.startsWith("/profile_")) {
                profiles += name.substring(9) + ",";
            } else if (name.startsWith("profile_")) {
                profiles += name.substring(8) + ",";
            }
        }
        file.close();
        file = root.openNextFile();
    }
    root.close();
    
    if (profiles.length() > 0) {
        profiles.remove(profiles.length() - 1);
    }
    return profiles;
}

File StorageManager::openFileForRead(const char* path) {
    return SD.open(path, FILE_READ);
}
