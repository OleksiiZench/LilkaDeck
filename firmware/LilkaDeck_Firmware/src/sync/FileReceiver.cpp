#include "sync/FileReceiver.h"
#include "util/Logger.h"

namespace {
constexpr const char* TAG = "SYNC";
}

FileReceiver::FileReceiver(ISerialLink& link) : _link(link) {}

void FileReceiver::begin(std::unique_ptr<IWritableFile> file, size_t expectedBytes) {
    _file = std::move(file);
    _expectedBytes = expectedBytes;
    _receivedBytes = 0;
    _chunkFilled = 0;
    _chunkTarget = 0;
    _lastActivityMs = millis();
}

void FileReceiver::reset() {
    _file.reset();
    _expectedBytes = 0;
    _chunkFilled = 0;
    _chunkTarget = 0;
}

bool FileReceiver::isReceiving() const {
    return _expectedBytes > 0;
}

bool FileReceiver::isStalled() const {
    return isReceiving() && (millis() - _lastActivityMs) > SyncProtocol::TRANSFER_TIMEOUT_MS;
}

void FileReceiver::update() {
    if (_chunkTarget == 0) {
        planNextChunk();
    }
    fillChunk();

    if (_chunkFilled == _chunkTarget) {
        commitChunk();
    }
}

void FileReceiver::planNextChunk() {
    size_t remaining = _expectedBytes - _receivedBytes;
    _chunkTarget = remaining < SyncProtocol::CHUNK_SIZE ? remaining : SyncProtocol::CHUNK_SIZE;
}

void FileReceiver::fillChunk() {
    while (_link.available() > 0 && _chunkFilled < _chunkTarget) {
        _chunk[_chunkFilled++] = static_cast<uint8_t>(_link.readByte());
        _lastActivityMs = millis();
    }
}

// An acknowledgement tells the host the chunk is on storage, so it is withheld on write failure.
void FileReceiver::commitChunk() {
    if (!_file || !_file->write(_chunk, _chunkFilled)) {
        Log::error(TAG, "Write failed at offset %u, aborting transfer", static_cast<unsigned>(_receivedBytes));
        reset();
        return;
    }

    _receivedBytes += _chunkFilled;
    _chunkFilled = 0;
    _chunkTarget = 0;

    if (_receivedBytes < _expectedBytes) {
        _link.writeLine(SyncProtocol::Reply::CHUNK_RECEIVED);
    } else {
        completeTransfer();
    }
}

void FileReceiver::completeTransfer() {
    _file.reset();
    _expectedBytes = 0;
    _link.writeLine(SyncProtocol::Reply::FILE_RECEIVED);
}
