#pragma once

#include <Arduino.h>
#include "input/ButtonId.h"
#include "input/Debouncer.h"

class IButtonEventHandler {
public:
    virtual ~IButtonEventHandler() = default;

    virtual void onButtonPressed(ButtonId button) = 0;
    virtual void onButtonReleased(ButtonId button) = 0;
};

// Polls the physical buttons and reports debounced press and release events.
class ButtonReader {
public:
    ButtonReader();

    void begin();
    void poll(IButtonEventHandler& handler);

private:
    struct Channel {
        ButtonId id = ButtonId::Up;
        uint8_t pin = 0;
        Debouncer debouncer;
    };

    Channel _channels[BUTTON_COUNT];
};
