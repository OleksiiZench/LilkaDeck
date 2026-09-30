#pragma once

#include <Arduino.h>
#include <memory>
#include <vector>

class IReadableFile {
public:
    virtual ~IReadableFile() = default;
    virtual size_t size() = 0;
    // Returns 0 once the end of the file is reached.
    virtual size_t read(uint8_t* buffer, size_t length) = 0;
};

class IWritableFile {
public:
    virtual ~IWritableFile() = default;
    virtual bool write(const uint8_t* data, size_t length) = 0;
};

// Files opened through this interface are closed when their handle is destroyed.
class IFileSystem {
public:
    virtual ~IFileSystem() = default;

    virtual bool readFile(const char* path, uint8_t* buffer, size_t capacity) = 0;
    virtual String readText(const char* path) = 0;
    virtual bool writeText(const char* path, const char* content) = 0;

    virtual bool createDirectory(const char* path) = 0;
    virtual bool removeDirectory(const char* path) = 0;
    virtual bool rename(const char* fromPath, const char* toPath) = 0;
    virtual std::vector<String> listDirectoryNames(const char* path) = 0;

    virtual std::unique_ptr<IReadableFile> openForRead(const char* path) = 0;
    virtual std::unique_ptr<IWritableFile> openForWrite(const char* path) = 0;
};
