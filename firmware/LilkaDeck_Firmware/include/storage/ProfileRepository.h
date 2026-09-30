#pragma once

#include <Arduino.h>
#include <memory>
#include <vector>
#include "storage/IFileSystem.h"

// Knows how profiles are laid out on storage: /profile_<id>/config.json plus icon files.
class ProfileRepository {
public:
    explicit ProfileRepository(IFileSystem& fileSystem);

    uint8_t countProfiles();
    std::vector<uint8_t> listProfileIds();

    String readConfig(uint8_t profileId);
    bool readIcon(uint8_t profileId, const String& iconName, uint8_t* buffer, size_t capacity);

    bool createProfile(uint8_t profileId);
    bool ensureProfileDirectory(uint8_t profileId);
    bool deleteAndCloseGap(uint8_t profileId, uint8_t profileCount);

    std::unique_ptr<IReadableFile> openFileForRead(uint8_t profileId, const String& fileName);
    std::unique_ptr<IWritableFile> openFileForWrite(uint8_t profileId, const String& fileName);

private:
    IFileSystem& _fileSystem;

    static String directoryPath(uint8_t profileId);
    static String filePath(uint8_t profileId, const String& fileName);
    void renameProfile(uint8_t fromId, uint8_t toId);
};
