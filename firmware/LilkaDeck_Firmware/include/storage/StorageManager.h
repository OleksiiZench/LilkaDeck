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
    
    // Utility to write simple text (used for generating default config.json)
    bool writeTextFile(const char* path, const char* content);

    // Sync Protocol Methods
    bool createDir(const char* path);
    bool openFileForWrite(const char* path);
    bool writeChunk(const uint8_t* data, size_t len);
    void closeFile();

    String getProfilesList();
    File openFileForRead(const char* path);
    
    // Advanced Profile Management Methods
    bool deleteDirRecursive(const char* path);
    bool renameFileOrDir(const char* oldPath, const char* newPath);

private:
    bool _isMounted;
    File _activeWriteFile; // Triggers progressive binary write
};
