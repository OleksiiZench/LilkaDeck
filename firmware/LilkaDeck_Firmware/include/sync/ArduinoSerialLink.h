#pragma once

#include "sync/ISerialLink.h"

class ArduinoSerialLink : public ISerialLink {
public:
    void begin();

    size_t available() override;
    int readByte() override;
    String readLine() override;
    void writeLine(const String& line) override;
    void writeBytes(const uint8_t* data, size_t length) override;
};
