#include "core/SyncManager.h"

SyncManager::SyncManager(StorageManager& storageManager, ProfileManager& profileManager)
    : _storageManager(storageManager), _profileManager(profileManager),
      _isSyncing(false), _lastSyncTime(0), _syncProfileId(0), 
      _expectedBytes(0), _receivedBytes(0), _chunkIndex(0), _currentChunkTarget(0) {
}

void SyncManager::begin() {
    Serial.begin(115200);
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
        if (millis() - _lastSyncTime > 3000) {
            Serial.println("[ERR] Sync timeout! Resetting state.");
            resetState();
        }
    }
}

void SyncManager::resetState() {
    _isSyncing = false;
    _expectedBytes = 0;
    _storageManager.closeFile();
}

void SyncManager::handleBinaryMode() {
    if (_currentChunkTarget == 0) {
        _currentChunkTarget = _expectedBytes - _receivedBytes;
        if (_currentChunkTarget > 256) _currentChunkTarget = 256;
    }

    while (Serial.available() > 0 && _chunkIndex < _currentChunkTarget) {
        _chunkBuffer[_chunkIndex++] = Serial.read();
        _lastSyncTime = millis();
    }

    if (_chunkIndex == _currentChunkTarget) {
        _storageManager.writeChunk(_chunkBuffer, _chunkIndex);
        _receivedBytes += _chunkIndex;
        
        _chunkIndex = 0;
        _currentChunkTarget = 0;

        if (_receivedBytes < _expectedBytes) {
            Serial.println("ACK_CHUNK");
        } else {
            _storageManager.closeFile();
            _expectedBytes = 0; 
            Serial.println("ACK_DONE");
        }
    }
}

void SyncManager::handleTextMode() {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    if (cmd.length() == 0) return;

    _lastSyncTime = millis();

    if (cmd == "PING") {
        Serial.println("LILKA_PONG:v1.0");
        return;
    }

    if (cmd.startsWith("SYNC_START:")) {
        _syncProfileId = cmd.substring(11).toInt();
        _isSyncing = true;
        _expectedBytes = 0;
        
        String dir = "/profile_" + String(_syncProfileId);
        _storageManager.createDir(dir.c_str());
        Serial.println("ACK_SYNC");
    }

    if (cmd.startsWith("SYNC_START:")) {
        _syncProfileId = cmd.substring(11).toInt();
        _isSyncing = true;
        _expectedBytes = 0;
        
        String dir = "/profile_" + String(_syncProfileId);
        _storageManager.createDir(dir.c_str());
        Serial.println("ACK_SYNC");
    }
    else if (_isSyncing && cmd.startsWith("FILE_START:")) {
        int firstColon = cmd.indexOf(':');
        int secondColon = cmd.indexOf(':', firstColon + 1);
        
        String fileName = cmd.substring(firstColon + 1, secondColon);
        _expectedBytes = cmd.substring(secondColon + 1).toInt();
        _receivedBytes = 0;
        
        _chunkIndex = 0; 
        _currentChunkTarget = 0;
        
        String filePath = "/profile_" + String(_syncProfileId) + "/" + fileName;
        _storageManager.openFileForWrite(filePath.c_str());
        
        Serial.println("ACK_FILE");
    }
    else if (_isSyncing && cmd == "SYNC_END") {
        resetState();
        Serial.println("ACK_END");
        
        _profileManager.loadProfile(_syncProfileId);
    }
}
