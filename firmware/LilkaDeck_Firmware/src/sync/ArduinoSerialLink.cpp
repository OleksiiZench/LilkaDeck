#include "sync/ArduinoSerialLink.h"
#include "sync/SyncProtocol.h"

void ArduinoSerialLink::begin() {
    Serial.begin(SyncProtocol::BAUD_RATE);
    // A short timeout keeps a partially received line from stalling the main loop.
    Serial.setTimeout(SyncProtocol::LINE_READ_TIMEOUT_MS);
}

size_t ArduinoSerialLink::available() {
    return static_cast<size_t>(Serial.available());
}

int ArduinoSerialLink::readByte() {
    return Serial.read();
}

String ArduinoSerialLink::readLine() {
    String line = Serial.readStringUntil('\n');
    line.trim();
    return line;
}

void ArduinoSerialLink::writeLine(const String& line) {
    Serial.println(line);
}

void ArduinoSerialLink::writeBytes(const uint8_t* data, size_t length) {
    Serial.write(data, length);
}
