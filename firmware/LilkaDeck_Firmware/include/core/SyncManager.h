#pragma once

#include <Arduino.h>
#include <memory>
#include "storage/ProfileRepository.h"
#include "core/ProfileManager.h"

class SyncManager {
public:
    SyncManager(ProfileRepository& repository, ProfileManager& profileManager);
    void begin();
    void update();
    bool isBusy() const;

private:
    ProfileRepository& _repository;
    ProfileManager& _profileManager;
    std::unique_ptr<IWritableFile> _activeFile;

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
    void streamFileToHost(uint8_t profileId, const String& fileName);
    void resetState();
};
