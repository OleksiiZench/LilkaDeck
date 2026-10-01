#include "sync/SyncCommand.h"
#include <string.h>

namespace {

struct KeywordCommand {
    const char* keyword;
    SyncCommandType type;
};

constexpr KeywordCommand KEYWORD_COMMANDS[] = {
    {"PING", SyncCommandType::Ping},
    {"GET_PROFILES", SyncCommandType::GetProfiles},
    {"PROFILE_CREATE", SyncCommandType::CreateProfile},
    {"SYNC_END", SyncCommandType::EndSync},
};

constexpr const char* DELETE_PROFILE_PREFIX = "PROFILE_DELETE:";
constexpr const char* SET_COLOR_PREFIX = "SET_COLOR:";
constexpr const char* START_SYNC_PREFIX = "SYNC_START:";
constexpr const char* GET_FILE_PREFIX = "FILE_GET:";
constexpr const char* START_FILE_PREFIX = "FILE_START:";

bool takeArgument(const String& line, const char* prefix, String& outArgument) {
    if (!line.startsWith(prefix)) return false;
    outArgument = line.substring(strlen(prefix));
    return true;
}

// Splits "<head>:<tail>" at the first colon; the tail may itself contain colons.
bool splitAtFirstColon(const String& text, String& outHead, String& outTail) {
    int colon = text.indexOf(':');
    if (colon < 0) return false;

    outHead = text.substring(0, colon);
    outTail = text.substring(colon + 1);
    return true;
}

bool parseKeyword(const String& line, SyncCommand& command) {
    for (const KeywordCommand& entry : KEYWORD_COMMANDS) {
        if (line == entry.keyword) {
            command.type = entry.type;
            return true;
        }
    }
    return false;
}

bool parseGetFile(const String& argument, SyncCommand& command) {
    String profileId;
    if (!splitAtFirstColon(argument, profileId, command.fileName)) return false;

    command.type = SyncCommandType::GetFile;
    command.profileId = profileId.toInt();
    return true;
}

bool parseStartFile(const String& argument, SyncCommand& command) {
    String fileSize;
    if (!splitAtFirstColon(argument, command.fileName, fileSize)) return false;

    command.type = SyncCommandType::StartFile;
    command.fileSize = fileSize.toInt();
    return true;
}

bool parseWithArgument(const String& line, SyncCommand& command) {
    String argument;

    if (takeArgument(line, DELETE_PROFILE_PREFIX, argument)) {
        command.type = SyncCommandType::DeleteProfile;
        command.profileId = argument.toInt();
        return true;
    }
    if (takeArgument(line, SET_COLOR_PREFIX, argument)) {
        command.type = SyncCommandType::SetColor;
        command.colorHex = argument;
        return true;
    }
    if (takeArgument(line, START_SYNC_PREFIX, argument)) {
        command.type = SyncCommandType::StartSync;
        command.profileId = argument.toInt();
        return true;
    }
    if (takeArgument(line, GET_FILE_PREFIX, argument)) return parseGetFile(argument, command);
    if (takeArgument(line, START_FILE_PREFIX, argument)) return parseStartFile(argument, command);
    return false;
}

}

SyncCommand parseSyncCommand(const String& line) {
    SyncCommand command;
    if (parseKeyword(line, command) || parseWithArgument(line, command)) {
        return command;
    }
    return SyncCommand();
}
