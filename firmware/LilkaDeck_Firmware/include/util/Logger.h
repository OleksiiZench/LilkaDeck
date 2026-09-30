#pragma once

namespace Log {

void info(const char* tag, const char* format, ...) __attribute__((format(printf, 2, 3)));
void error(const char* tag, const char* format, ...) __attribute__((format(printf, 2, 3)));

}
