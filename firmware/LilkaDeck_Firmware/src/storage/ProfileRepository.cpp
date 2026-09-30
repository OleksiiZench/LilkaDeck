#include "storage/ProfileRepository.h"
#include <string.h>
#include "util/Logger.h"

namespace {

constexpr const char* TAG = "PROFILE";
constexpr const char* DIRECTORY_PREFIX = "profile_";
constexpr const char* CONFIG_FILE_NAME = "config.json";
constexpr uint8_t MAX_PROFILE_COUNT = 255;

constexpr const char* DEFAULT_CONFIG = R"({
  "profileName": "New Profile",
  "activeColor": "#00FFFF",
  "buttons": {}
})";

bool isAllDigits(const String& text) {
    for (size_t i = 0; i < text.length(); i++) {
        if (!isDigit(text[i])) return false;
    }
    return text.length() > 0;
}

bool tryParseProfileId(const String& directoryName, uint8_t& outId) {
    if (!directoryName.startsWith(DIRECTORY_PREFIX)) return false;

    String digits = directoryName.substring(strlen(DIRECTORY_PREFIX));
    if (!isAllDigits(digits)) return false;

    long value = digits.toInt();
    if (value > MAX_PROFILE_COUNT) return false;

    outId = static_cast<uint8_t>(value);
    return true;
}

}

ProfileRepository::ProfileRepository(IFileSystem& fileSystem) : _fileSystem(fileSystem) {}

uint8_t ProfileRepository::countProfiles() {
    uint8_t count = 0;
    while (count < MAX_PROFILE_COUNT && readConfig(count).length() > 0) {
        count++;
    }
    return count;
}

std::vector<uint8_t> ProfileRepository::listProfileIds() {
    std::vector<uint8_t> ids;
    for (const String& name : _fileSystem.listDirectoryNames("/")) {
        uint8_t id;
        if (tryParseProfileId(name, id)) {
            ids.push_back(id);
        }
    }
    return ids;
}

String ProfileRepository::readConfig(uint8_t profileId) {
    return _fileSystem.readText(filePath(profileId, CONFIG_FILE_NAME).c_str());
}

bool ProfileRepository::readIcon(uint8_t profileId, const String& iconName, uint8_t* buffer, size_t capacity) {
    return _fileSystem.readFile(filePath(profileId, iconName).c_str(), buffer, capacity);
}

bool ProfileRepository::ensureProfileDirectory(uint8_t profileId) {
    return _fileSystem.createDirectory(directoryPath(profileId).c_str());
}

bool ProfileRepository::createProfile(uint8_t profileId) {
    return ensureProfileDirectory(profileId)
        && _fileSystem.writeText(filePath(profileId, CONFIG_FILE_NAME).c_str(), DEFAULT_CONFIG);
}

bool ProfileRepository::deleteAndCloseGap(uint8_t profileId, uint8_t profileCount) {
    if (!_fileSystem.removeDirectory(directoryPath(profileId).c_str())) {
        Log::error(TAG, "Failed to delete profile_%d", profileId);
        return false;
    }
    Log::info(TAG, "Deleted profile_%d", profileId);

    for (uint8_t id = profileId + 1; id < profileCount; id++) {
        renameProfile(id, id - 1);
    }
    return true;
}

std::unique_ptr<IReadableFile> ProfileRepository::openFileForRead(uint8_t profileId, const String& fileName) {
    return _fileSystem.openForRead(filePath(profileId, fileName).c_str());
}

std::unique_ptr<IWritableFile> ProfileRepository::openFileForWrite(uint8_t profileId, const String& fileName) {
    return _fileSystem.openForWrite(filePath(profileId, fileName).c_str());
}

String ProfileRepository::directoryPath(uint8_t profileId) {
    return String("/") + DIRECTORY_PREFIX + String(profileId);
}

String ProfileRepository::filePath(uint8_t profileId, const String& fileName) {
    return directoryPath(profileId) + "/" + fileName;
}

void ProfileRepository::renameProfile(uint8_t fromId, uint8_t toId) {
    bool renamed = _fileSystem.rename(directoryPath(fromId).c_str(), directoryPath(toId).c_str());
    if (renamed) {
        Log::info(TAG, "Renamed profile_%d to profile_%d", fromId, toId);
    } else {
        Log::error(TAG, "Failed to rename profile_%d to profile_%d", fromId, toId);
    }
}
