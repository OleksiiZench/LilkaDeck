#pragma once

#include <memory>
#include "storage/IFileSystem.h"
#include "sync/ISerialLink.h"
#include "sync/SyncProtocol.h"

// Receives one file from the host as acknowledged chunks and writes it to storage.
class FileReceiver {
public:
    explicit FileReceiver(ISerialLink& link);

    void begin(std::unique_ptr<IWritableFile> file, size_t expectedBytes);
    // Consumes whatever bytes are available; call only while isReceiving().
    void update();
    // Closes the current file and abandons any transfer in progress.
    void reset();

    bool isReceiving() const;
    bool isStalled() const;

private:
    ISerialLink& _link;
    std::unique_ptr<IWritableFile> _file;
    size_t _expectedBytes = 0;
    size_t _receivedBytes = 0;
    uint8_t _chunk[SyncProtocol::CHUNK_SIZE];
    size_t _chunkFilled = 0;
    size_t _chunkTarget = 0;
    unsigned long _lastActivityMs = 0;

    void planNextChunk();
    void fillChunk();
    void commitChunk();
    void completeTransfer();
};
