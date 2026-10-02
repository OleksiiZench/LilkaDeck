#include "util/Logger.h"
#include <Arduino.h>
#include <cstdarg>

namespace {

constexpr size_t MAX_MESSAGE_LENGTH = 160;

void print(const char* tag, const char* levelPrefix, const char* format, va_list args) {
    char message[MAX_MESSAGE_LENGTH];
    vsnprintf(message, sizeof(message), format, args);
    Serial.printf("[%s] %s%s\n", tag, levelPrefix, message);
}

}

void Log::info(const char* tag, const char* format, ...) {
    va_list args;
    va_start(args, format);
    print(tag, "", format, args);
    va_end(args);
}

void Log::warn(const char* tag, const char* format, ...) {
    va_list args;
    va_start(args, format);
    print(tag, "WARN: ", format, args);
    va_end(args);
}

void Log::error(const char* tag, const char* format, ...) {
    va_list args;
    va_start(args, format);
    print(tag, "ERROR: ", format, args);
    va_end(args);
}
