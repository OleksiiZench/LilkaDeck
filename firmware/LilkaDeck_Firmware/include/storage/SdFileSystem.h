#pragma once

#include <SPI.h>
#include "storage/IFileSystem.h"

class SdFileSystem : public IFileSystem {
public:
    bool begin(SPIClass& spiBus, uint8_t csPin);

    bool readFile(const char* path, uint8_t* buffer, size_t capacity) override;
    String readText(const char* path) override;
    bool writeText(const char* path, const char* content) override;

    bool createDirectory(const char* path) override;
    bool removeDirectory(const char* path) override;
    bool rename(const char* fromPath, const char* toPath) override;
    std::vector<String> listDirectoryNames(const char* path) override;

    std::unique_ptr<IReadableFile> openForRead(const char* path) override;
    std::unique_ptr<IWritableFile> openForWrite(const char* path) override;

private:
    bool _isMounted = false;
    uint8_t _csPin = 0;
};
