#pragma once
#include <SPI.h>
#include <SD.h>

class StorageManager {
public:
    StorageManager();
    
    // Explicitly inject the shared SPI bus and CS pin
    bool begin(SPIClass& spiBus, uint8_t csPin);

private:
    bool _isMounted;
};
