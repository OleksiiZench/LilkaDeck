#include "storage/StorageManager.h"
#include <Arduino.h>

StorageManager::StorageManager() : _isMounted(false) {
}

bool StorageManager::begin(SPIClass& spiBus, uint8_t csPin) {
    // SD.begin calls spiBus.begin(), which safely returns early 
    // since DisplayManager already initialized this specific SPIClass instance.
    if (!SD.begin(csPin, spiBus)) {
        Serial0.println("[ERROR] StorageManager: SD Mount Failed!");
        return false;
    }
    
    uint8_t cardType = SD.cardType();
    if (cardType == CARD_NONE) {
        Serial0.println("[ERROR] StorageManager: No SD card attached.");
        return false;
    }

    _isMounted = true;
    Serial0.printf("[SYS] StorageManager: SD Card Initialized. Type: %d\n", cardType);
    
    return true;
}

bool StorageManager::readFileToBuffer(const char* path, uint8_t* buffer, size_t bufferSize) {
    if (!_isMounted) {
        Serial0.println("[ERROR] StorageManager: Cannot read, SD not mounted.");
        return false;
    }

    File file = SD.open(path, FILE_READ);
    if (!file) {
        Serial0.printf("[ERROR] StorageManager: Failed to open file %s\n", path);
        return false;
    }

    size_t fileSize = file.size();
    if (fileSize > bufferSize) {
        Serial0.printf("[WARN] StorageManager: File %s is larger than buffer. Truncating.\n", path);
        fileSize = bufferSize;
    }

    size_t bytesRead = file.read(buffer, fileSize);
    file.close();

    if (bytesRead != fileSize) {
        Serial0.println("[ERROR] StorageManager: File read incomplete.");
        return false;
    }

    return true;
}
