#pragma once

#include <Arduino.h>
#include "core/ProfileManager.h"
#include "storage/ProfileRepository.h"
#include "sync/FileReceiver.h"
#include "sync/ISerialLink.h"
#include "sync/SyncCommand.h"

// Executes commands sent by the desktop app over the serial link.
class SyncManager {
public:
    SyncManager(ISerialLink& link, ProfileRepository& repository,
                ProfileManager& profileManager, FileReceiver& receiver);

    void update();
    bool isBusy() const;

private:
    ISerialLink& _link;
    ProfileRepository& _repository;
    ProfileManager& _profileManager;
    FileReceiver& _receiver;

    bool _isSyncing = false;
    uint8_t _syncProfileId = 0;

    void abortIfTransferStalled();
    void handleNextCommand();
    void dispatch(const SyncCommand& command);

    void onGetProfiles();
    void onCreateProfile();
    void onDeleteProfile(uint8_t profileId);
    void onGetFile(uint8_t profileId, const String& fileName);
    void onStartSync(uint8_t profileId);
    void onStartFile(const String& fileName, size_t fileSize);
    void onEndSync();

    void sendFileContents(IReadableFile& file);
    void closeSession();
};
