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
