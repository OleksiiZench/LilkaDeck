#include "storage/SdFileSystem.h"
#include <SD.h>
#include "util/Logger.h"

namespace {

constexpr const char* TAG = "STORAGE";

// The SD chip select must be released after every operation, otherwise
// the TFT driver deadlocks on the shared SPI bus at its next draw command.
class ChipSelectRelease {
public:
    explicit ChipSelectRelease(uint8_t csPin) : _csPin(csPin) { digitalWrite(_csPin, HIGH); }
    ~ChipSelectRelease() { digitalWrite(_csPin, HIGH); }

private:
    uint8_t _csPin;
};

class SdReadableFile : public IReadableFile {
public:
    SdReadableFile(File file, uint8_t csPin) : _file(file), _csPin(csPin) {}

    ~SdReadableFile() override {
        ChipSelectRelease release(_csPin);
        _file.close();
    }

    size_t size() override { return _file.size(); }

    size_t read(uint8_t* buffer, size_t length) override {
        ChipSelectRelease release(_csPin);
        return _file.read(buffer, length);
    }

private:
    File _file;
    uint8_t _csPin;
};

class SdWritableFile : public IWritableFile {
public:
    SdWritableFile(File file, uint8_t csPin) : _file(file), _csPin(csPin) {}

    ~SdWritableFile() override {
        ChipSelectRelease release(_csPin);
        _file.close();
    }

    bool write(const uint8_t* data, size_t length) override {
        ChipSelectRelease release(_csPin);
        return _file.write(data, length) == length;
    }

private:
    File _file;
    uint8_t _csPin;
};

void removeIfExists(const char* path) {
    if (SD.exists(path)) {
        SD.remove(path);
    }
}

// Rewinds after each removal because deleting entries invalidates the FAT iteration order.
bool removeNextFile(File& directory, const char* directoryPath) {
    File entry = directory.openNextFile();
    if (!entry) return false;

    String entryPath = String(directoryPath) + "/" + entry.name();
    entry.close();

    bool removed = SD.remove(entryPath.c_str());
    directory.rewindDirectory();
    return removed;
}

String withoutLeadingSlash(const char* name) {
    String result(name);
    return result.startsWith("/") ? result.substring(1) : result;
}

}

bool SdFileSystem::begin(SPIClass& spiBus, uint8_t csPin) {
    _csPin = csPin;

    if (!SD.begin(csPin, spiBus)) {
        Log::error(TAG, "SD mount failed");
        return false;
    }

    uint8_t cardType = SD.cardType();
    if (cardType == CARD_NONE) {
        Log::error(TAG, "No SD card attached");
        return false;
    }

    _isMounted = true;
    Log::info(TAG, "SD card initialized, type: %d", cardType);
    return true;
}

bool SdFileSystem::readFile(const char* path, uint8_t* buffer, size_t capacity) {
    if (!_isMounted) return false;
    ChipSelectRelease release(_csPin);

    File file = SD.open(path, FILE_READ);
    if (!file) return false;

    size_t fileSize = file.size();
    size_t bytesToRead = fileSize < capacity ? fileSize : capacity;
    size_t bytesRead = file.read(buffer, bytesToRead);
    file.close();

    return bytesRead == bytesToRead;
}

String SdFileSystem::readText(const char* path) {
    if (!_isMounted) return "";
    ChipSelectRelease release(_csPin);

    File file = SD.open(path);
    if (!file) return "";

    String content = file.readString();
    file.close();
    return content;
}

bool SdFileSystem::writeText(const char* path, const char* content) {
    if (!_isMounted) return false;
    ChipSelectRelease release(_csPin);

    removeIfExists(path);
    File file = SD.open(path, FILE_WRITE);
    if (!file) return false;

    file.print(content);
    file.close();
    return true;
}

bool SdFileSystem::createDirectory(const char* path) {
    if (!_isMounted) return false;
    ChipSelectRelease release(_csPin);

    return SD.exists(path) || SD.mkdir(path);
}

bool SdFileSystem::removeDirectory(const char* path) {
    if (!_isMounted) return false;
    ChipSelectRelease release(_csPin);

    File directory = SD.open(path);
    if (!directory) return false;

    while (removeNextFile(directory, path)) {}
    directory.close();

    return SD.rmdir(path);
}

bool SdFileSystem::rename(const char* fromPath, const char* toPath) {
    if (!_isMounted) return false;
    ChipSelectRelease release(_csPin);

    return SD.rename(fromPath, toPath);
}

std::vector<String> SdFileSystem::listDirectoryNames(const char* path) {
    std::vector<String> names;
    if (!_isMounted) return names;
    ChipSelectRelease release(_csPin);

    File root = SD.open(path);
    if (!root) return names;

    for (File entry = root.openNextFile(); entry; entry = root.openNextFile()) {
        if (entry.isDirectory()) {
            names.push_back(withoutLeadingSlash(entry.name()));
        }
        entry.close();
    }
    root.close();
    return names;
}

std::unique_ptr<IReadableFile> SdFileSystem::openForRead(const char* path) {
    if (!_isMounted) return nullptr;
    ChipSelectRelease release(_csPin);

    File file = SD.open(path, FILE_READ);
    if (!file || file.isDirectory()) return nullptr;

    return std::unique_ptr<IReadableFile>(new SdReadableFile(file, _csPin));
}

std::unique_ptr<IWritableFile> SdFileSystem::openForWrite(const char* path) {
    if (!_isMounted) return nullptr;
    ChipSelectRelease release(_csPin);

    removeIfExists(path);
    File file = SD.open(path, FILE_WRITE);
    if (!file) return nullptr;

    return std::unique_ptr<IWritableFile>(new SdWritableFile(file, _csPin));
}
