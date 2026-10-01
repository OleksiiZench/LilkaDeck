#include "sync/SyncManager.h"
#include "sync/SyncProtocol.h"
#include "util/Logger.h"

namespace {

constexpr const char* TAG = "SYNC";

String joinWithCommas(const std::vector<uint8_t>& ids) {
    String joined;
    for (size_t i = 0; i < ids.size(); i++) {
        if (i > 0) joined += ',';
        joined += String(ids[i]);
    }
    return joined;
}

}

SyncManager::SyncManager(ISerialLink& link, ProfileRepository& repository,
                         ProfileManager& profileManager, FileReceiver& receiver)
    : _link(link), _repository(repository), _profileManager(profileManager), _receiver(receiver) {}

bool SyncManager::isBusy() const {
    return _isSyncing;
}

void SyncManager::update() {
    abortIfTransferStalled();

    if (_link.available() == 0) return;

    if (_isSyncing && _receiver.isReceiving()) {
        _receiver.update();
    } else {
        handleNextCommand();
    }
}

void SyncManager::abortIfTransferStalled() {
    if (_isSyncing && _receiver.isStalled()) {
        Log::error(TAG, "Sync timeout, resetting state");
        closeSession();
    }
}

void SyncManager::handleNextCommand() {
    String line = _link.readLine();
    if (line.length() == 0) return;

    dispatch(parseSyncCommand(line));
}

void SyncManager::dispatch(const SyncCommand& command) {
    switch (command.type) {
        case SyncCommandType::Ping:
            _link.writeLine(SyncProtocol::Reply::PONG);
            break;
        case SyncCommandType::GetProfiles:
            onGetProfiles();
            break;
        case SyncCommandType::CreateProfile:
            onCreateProfile();
            break;
        case SyncCommandType::DeleteProfile:
            onDeleteProfile(command.profileId);
            break;
        case SyncCommandType::SetColor:
            _profileManager.previewColor(command.colorHex);
            break;
        case SyncCommandType::GetFile:
            onGetFile(command.profileId, command.fileName);
            break;
        case SyncCommandType::StartSync:
            onStartSync(command.profileId);
            break;
        case SyncCommandType::StartFile:
            if (_isSyncing) onStartFile(command.fileName, command.fileSize);
            break;
        case SyncCommandType::EndSync:
            if (_isSyncing) onEndSync();
            break;
        case SyncCommandType::Unknown:
            break;
    }
}

void SyncManager::onGetProfiles() {
    String ids = joinWithCommas(_repository.listProfileIds());
    _link.writeLine(String(SyncProtocol::Reply::PROFILE_LIST) + ids);
}

void SyncManager::onCreateProfile() {
    uint8_t newId = _profileManager.createNewProfile();
    _link.writeLine(String(SyncProtocol::Reply::PROFILE_CREATED) + String(newId));
}

void SyncManager::onDeleteProfile(uint8_t profileId) {
    bool deleted = _profileManager.deleteProfile(profileId);
    _link.writeLine(deleted ? SyncProtocol::Reply::PROFILE_DELETED : SyncProtocol::Reply::CANNOT_DELETE);
}

void SyncManager::onGetFile(uint8_t profileId, const String& fileName) {
    std::unique_ptr<IReadableFile> file = _repository.openFileForRead(profileId, fileName);
    if (!file) {
        _link.writeLine(SyncProtocol::Reply::FILE_NOT_FOUND);
        return;
    }

    unsigned long fileSize = static_cast<unsigned long>(file->size());
    _link.writeLine(String(SyncProtocol::Reply::FILE_SEND_START) + String(fileSize));
    sendFileContents(*file);
}

// Nothing may be logged while streaming: the host reads raw bytes, and a stray line would corrupt the file.
void SyncManager::sendFileContents(IReadableFile& file) {
    uint8_t buffer[SyncProtocol::CHUNK_SIZE];
    size_t bytesRead;
    while ((bytesRead = file.read(buffer, sizeof(buffer))) > 0) {
        _link.writeBytes(buffer, bytesRead);
    }
}

void SyncManager::onStartSync(uint8_t profileId) {
    _syncProfileId = profileId;
    _isSyncing = true;
    _receiver.reset();

    _repository.ensureProfileDirectory(profileId);
    _link.writeLine(SyncProtocol::Reply::SYNC_STARTED);
}

// Without the acknowledgement the host reports a failure, which is better than silently losing the file.
void SyncManager::onStartFile(const String& fileName, size_t fileSize) {
    _receiver.reset();

    std::unique_ptr<IWritableFile> file = _repository.openFileForWrite(_syncProfileId, fileName);
    if (!file) {
        Log::error(TAG, "Cannot open %s for writing", fileName.c_str());
        return;
    }

    _receiver.begin(std::move(file), fileSize);
    _link.writeLine(SyncProtocol::Reply::FILE_ACCEPTED);
}

void SyncManager::onEndSync() {
    closeSession();
    _link.writeLine(SyncProtocol::Reply::SYNC_ENDED);

    _profileManager.reloadProfile(_syncProfileId);
}

void SyncManager::closeSession() {
    _isSyncing = false;
    _receiver.reset();
}
