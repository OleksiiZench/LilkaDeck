#pragma once

#include <Arduino.h>

// Media actions use the USB consumer-control page and carry the "MEDIA_" prefix.
bool isMediaAction(const String& action);

bool tryLookupKeyboardKey(const String& name, uint8_t& outKeycode);
bool tryLookupMediaKey(const String& action, uint16_t& outUsageCode);
