#pragma once

#include <Arduino.h>

class ISerialLink {
public:
    virtual ~ISerialLink() = default;

    virtual size_t available() = 0;
    virtual int readByte() = 0;
    // Reads up to the next newline and returns the line without surrounding whitespace.
    virtual String readLine() = 0;
    virtual void writeLine(const String& line) = 0;
    virtual void writeBytes(const uint8_t* data, size_t length) = 0;
};
