#pragma once

#include <Arduino.h>

enum class SyncCommandType {
    Unknown,
    Ping,
    GetProfiles,
    CreateProfile,
    DeleteProfile,
    SetColor,
    GetFile,
    StartSync,
    StartFile,
    EndSync
};

struct SyncCommand {
    SyncCommandType type = SyncCommandType::Unknown;
    uint8_t profileId = 0;
    String fileName;
    size_t fileSize = 0;
    String colorHex;
};

// Malformed lines yield a command of type Unknown.
SyncCommand parseSyncCommand(const String& line);
