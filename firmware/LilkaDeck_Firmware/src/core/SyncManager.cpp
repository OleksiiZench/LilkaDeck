#include "core/SyncManager.h"
#include "config/BoardConfig.h"

SyncManager::SyncManager(StorageManager& storageManager, ProfileManager& profileManager)
    : _storageManager(storageManager), _profileManager(profileManager),
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
    _storageManager.closeFile();
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
        _storageManager.writeChunk(_chunkBuffer, _chunkIndex);
        _receivedBytes += _chunkIndex;
        
        _chunkIndex = 0;
        _currentChunkTarget = 0;

        if (_receivedBytes < _expectedBytes) {
            // Acknowledge the chunk so the PC can send the next one
            Serial.println("ACK_CHUNK");
        } else {
            // Transfer complete
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

    // Auto-connect handshake
    if (cmd == "PING") {
        Serial.println("LILKA_PONG:v1.0");
        return;
    }

    // Host requested the list of available profiles
    if (cmd == "GET_PROFILES") {
        String profiles = _storageManager.getProfilesList();
        Serial.println("PROFILES:" + profiles);
        
        // Release the shared SPI bus after SD card operations.
        // This prevents hardware conflicts and ensures the TFT display can instantly 
        // draw frames (e.g., button outlines) when physical buttons are pressed.
        digitalWrite(BoardConfig::PIN_SD_CS, HIGH);
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
            String profileId = cmd.substring(firstColon + 1, secondColon);
            String fileName = cmd.substring(secondColon + 1);
            String filePath = "/profile_" + profileId + "/" + fileName;
            
            File file = _storageManager.openFileForRead(filePath.c_str());
            if (file && !file.isDirectory()) {
                size_t fileSize = file.size();
                
                // Notify the host about the incoming file size
                Serial.printf("FILE_SEND_START:%d\n", fileSize);
                
                // Stream the file raw bytes to the Serial port
                uint8_t buf[256];
                while (file.available()) {
                    size_t bytesRead = file.read(buf, sizeof(buf));
                    if (bytesRead > 0) {
                        Serial.write(buf, bytesRead);
                    }
                }
                file.close();
            } else {
                Serial.println("ERR:FILE_NOT_FOUND");
            }
        }
        
        // Free the SPI bus once the file transfer is complete.
        // Failing to do this causes the TFT driver to deadlock on the first draw command.
        digitalWrite(BoardConfig::PIN_SD_CS, HIGH);
        return;
    }

    // Host initiates a PC -> ESP32 synchronization process
    if (cmd.startsWith("SYNC_START:")) {
        _syncProfileId = cmd.substring(11).toInt();
        _isSyncing = true;
        _expectedBytes = 0;
        
        String dir = "/profile_" + String(_syncProfileId);
        _storageManager.createDir(dir.c_str());
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
        
        String filePath = "/profile_" + String(_syncProfileId) + "/" + fileName;
        _storageManager.openFileForWrite(filePath.c_str());
        
        Serial.println("ACK_FILE");
    }
    // Host successfully finished sending all data
    else if (_isSyncing && cmd == "SYNC_END") {
        resetState();
        Serial.println("ACK_END");
        
        // Reload the UI to reflect the newly synchronized data
        _profileManager.loadProfile(_syncProfileId);
    }
}
