#pragma once
#include <SPI.h>
#include <SD.h>

class StorageManager {
public:
    StorageManager();
    
    // Explicitly inject the shared SPI bus and CS pin
    bool begin(SPIClass& spiBus, uint8_t csPin);

    bool readFileToBuffer(const char* path, uint8_t* buffer, size_t bufferSize);
    String readTextFile(const char *path);

    // Sync Protocol Methods
    bool createDir(const char* path);
    bool openFileForWrite(const char* path);
    bool writeChunk(const uint8_t* data, size_t len);
    void closeFile();

    String getProfilesList();
    File openFileForRead(const char* path);

private:
    bool _isMounted;
    File _activeWriteFile; // Triggers progressive binary write
};