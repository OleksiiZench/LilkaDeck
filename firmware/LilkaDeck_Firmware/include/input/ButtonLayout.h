#pragma once

#include "domain/IconPosition.h"
#include "input/ButtonId.h"

// Select and Start are system buttons: they have no icon position and never trigger macros.
bool tryGetIconPosition(ButtonId button, IconPosition& outPosition);
