#pragma once

#include <Arduino.h>
#include "storage/StorageManager.h"
#include "core/ProfileManager.h"

class SyncManager {
public:
    SyncManager(StorageManager& storageManager, ProfileManager& profileManager);
    
    void begin();
    void update();
    bool isBusy() const;

private:
    StorageManager& _storageManager;
    ProfileManager& _profileManager;

    bool _isSyncing;
    unsigned long _lastSyncTime;
    uint8_t _syncProfileId;
    size_t _expectedBytes;
    size_t _receivedBytes;

    uint8_t _chunkBuffer[256];
    size_t _chunkIndex;
    size_t _currentChunkTarget;

    void checkTimeout();
    void handleBinaryMode();
    void handleTextMode();
    void resetState();
};
