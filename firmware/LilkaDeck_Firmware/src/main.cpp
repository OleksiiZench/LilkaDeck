#include <Arduino.h>
#include "app/Application.h"

namespace {
Application application;
}

void setup() {
    application.setup();
}

void loop() {
    application.loop();
}
