#pragma once

#include <stddef.h>
#include <stdint.h>

// Wire-level constants shared with the desktop app (LilkaCommunicationService).
// Changing any of them requires a matching change on the desktop side.
namespace SyncProtocol {

constexpr uint32_t BAUD_RATE = 115200;
constexpr uint32_t LINE_READ_TIMEOUT_MS = 50;

constexpr size_t CHUNK_SIZE = 256;
constexpr unsigned long TRANSFER_TIMEOUT_MS = 3000;

namespace Reply {
constexpr const char* PONG = "LILKA_PONG:v1.0";
constexpr const char* PROFILE_LIST = "PROFILES:";
constexpr const char* PROFILE_CREATED = "ACK_PROFILE_CREATE:";
constexpr const char* PROFILE_DELETED = "ACK_PROFILE_DELETE";
constexpr const char* CANNOT_DELETE = "ERR:CANNOT_DELETE";
constexpr const char* FILE_NOT_FOUND = "ERR:FILE_NOT_FOUND";
constexpr const char* FILE_SEND_START = "FILE_SEND_START:";
constexpr const char* SYNC_STARTED = "ACK_SYNC";
constexpr const char* FILE_ACCEPTED = "ACK_FILE";
constexpr const char* CHUNK_RECEIVED = "ACK_CHUNK";
constexpr const char* FILE_RECEIVED = "ACK_DONE";
constexpr const char* SYNC_ENDED = "ACK_END";
}

// Messages the device sends on its own initiative.
namespace Notification {
constexpr const char* EXECUTE = "EXECUTE:";
}

}
