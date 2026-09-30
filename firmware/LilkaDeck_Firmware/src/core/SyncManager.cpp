#include "core/SyncManager.h"

namespace {

String joinWithCommas(const std::vector<uint8_t>& ids) {
    String joined;
    for (size_t i = 0; i < ids.size(); i++) {
        if (i > 0) joined += ',';
        joined += String(ids[i]);
    }
    return joined;
}

}

SyncManager::SyncManager(ProfileRepository& repository, ProfileManager& profileManager)
    : _repository(repository), _profileManager(profileManager),
      _isSyncing(false), _lastSyncTime(0), _syncProfileId(0),
      _expectedBytes(0), _receivedBytes(0), _chunkIndex(0), _currentChunkTarget(0) {
}

void SyncManager::begin() {
    Serial.begin(115200);
    // Short timeout for fast binary reading without blocking the main loop
    Serial.setTimeout(50);
}

bool SyncManager::isBusy() const {
    return _isSyncing;
}

void SyncManager::update() {
    checkTimeout();

    if (!Serial.available()) return;

    if (_isSyncing && _expectedBytes > 0) {
        handleBinaryMode();
    } else {
        handleTextMode();
    }
}

void SyncManager::checkTimeout() {
    if (_isSyncing && _expectedBytes > 0) {
        // Abort synchronization if no data is received within the 3-second window
        if (millis() - _lastSyncTime > 3000) {
            Serial.println("[ERR] Sync timeout! Resetting state.");
            resetState();
        }
    }
}

void SyncManager::resetState() {
    _isSyncing = false;
    _expectedBytes = 0;
    _activeFile.reset();
}

void SyncManager::handleBinaryMode() {
    // Determine the size of the next chunk to read
    if (_currentChunkTarget == 0) {
        _currentChunkTarget = _expectedBytes - _receivedBytes;
        if (_currentChunkTarget > 256) _currentChunkTarget = 256;
    }

    // Read available bytes into the chunk buffer
    while (Serial.available() > 0 && _chunkIndex < _currentChunkTarget) {
        _chunkBuffer[_chunkIndex++] = Serial.read();
        _lastSyncTime = millis();
    }

    // Process the chunk once it is fully received
    if (_chunkIndex == _currentChunkTarget) {
        if (_activeFile) {
            _activeFile->write(_chunkBuffer, _chunkIndex);
        }
        _receivedBytes += _chunkIndex;

        _chunkIndex = 0;
        _currentChunkTarget = 0;

        if (_receivedBytes < _expectedBytes) {
            // Acknowledge the chunk so the PC can send the next one
            Serial.println("ACK_CHUNK");
        } else {
            // Transfer complete
            _activeFile.reset();
            _expectedBytes = 0;
            Serial.println("ACK_DONE");
        }
    }
}

void SyncManager::streamFileToHost(uint8_t profileId, const String& fileName) {
    std::unique_ptr<IReadableFile> file = _repository.openFileForRead(profileId, fileName);
    if (!file) {
        Serial.println("ERR:FILE_NOT_FOUND");
        return;
    }

    // Notify the host about the incoming file size
    Serial.printf("FILE_SEND_START:%u\n", static_cast<unsigned>(file->size()));

    uint8_t buffer[256];
    size_t bytesRead;
    while ((bytesRead = file->read(buffer, sizeof(buffer))) > 0) {
        Serial.write(buffer, bytesRead);
    }
}

void SyncManager::handleTextMode() {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    if (cmd.length() == 0) return;

    _lastSyncTime = millis();

    // Auto-connect handshake
    if (cmd == "PING") {
        Serial.println("LILKA_PONG:v1.0");
        return;
    }

    // Host requested the list of available profiles
    if (cmd == "GET_PROFILES") {
        Serial.println("PROFILES:" + joinWithCommas(_repository.listProfileIds()));
        return;
    }

    // Host requests creating a new profile
    if (cmd == "PROFILE_CREATE") {
        uint8_t newId = _profileManager.createNewProfile();
        Serial.printf("ACK_PROFILE_CREATE:%d\n", newId);
        return;
    }

    // Host requests deleting a specific profile
    if (cmd.startsWith("PROFILE_DELETE:")) {
        uint8_t idToDelete = cmd.substring(15).toInt();
        if (_profileManager.deleteProfile(idToDelete)) {
            Serial.println("ACK_PROFILE_DELETE");
        } else {
            Serial.println("ERR:CANNOT_DELETE");
        }
        return;
    }

    // Live preview for UI color (RAM update only)
    if (cmd.startsWith("SET_COLOR:")) {
        String hexColor = cmd.substring(10);
        _profileManager.previewColor(hexColor);
        return;
    }

    // Host requested a specific file from the SD card
    if (cmd.startsWith("FILE_GET:")) {
        int firstColon = cmd.indexOf(':');
        int secondColon = cmd.indexOf(':', firstColon + 1);

        if (firstColon != -1 && secondColon != -1) {
            uint8_t profileId = cmd.substring(firstColon + 1, secondColon).toInt();
            String fileName = cmd.substring(secondColon + 1);
            streamFileToHost(profileId, fileName);
        }
        return;
    }

    // Host initiates a PC -> ESP32 synchronization process
    if (cmd.startsWith("SYNC_START:")) {
        _syncProfileId = cmd.substring(11).toInt();
        _isSyncing = true;
        _expectedBytes = 0;

        _repository.ensureProfileDirectory(_syncProfileId);
        Serial.println("ACK_SYNC");
    }
    // Host is preparing to send a file to the ESP32
    else if (_isSyncing && cmd.startsWith("FILE_START:")) {
        int firstColon = cmd.indexOf(':');
        int secondColon = cmd.indexOf(':', firstColon + 1);

        String fileName = cmd.substring(firstColon + 1, secondColon);
        _expectedBytes = cmd.substring(secondColon + 1).toInt();
        _receivedBytes = 0;

        _chunkIndex = 0;
        _currentChunkTarget = 0;

        _activeFile.reset();
        _activeFile = _repository.openFileForWrite(_syncProfileId, fileName);

        Serial.println("ACK_FILE");
    }
    // Host successfully finished sending all data
    else if (_isSyncing && cmd == "SYNC_END") {
        resetState();
        Serial.println("ACK_END");

        // Reload the UI to reflect the newly synchronized data
        _profileManager.loadProfile(_syncProfileId, false);
    }
}
