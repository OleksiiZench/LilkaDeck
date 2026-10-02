#include "input/ButtonReader.h"
#include "config/BoardConfig.h"

namespace {

struct ButtonPin {
    ButtonId id;
    uint8_t pin;
};

constexpr ButtonPin BUTTON_PINS[] = {
    {ButtonId::Up,     BoardConfig::PIN_BUTTON_UP},
    {ButtonId::Down,   BoardConfig::PIN_BUTTON_DOWN},
    {ButtonId::Left,   BoardConfig::PIN_BUTTON_LEFT},
    {ButtonId::Right,  BoardConfig::PIN_BUTTON_RIGHT},
    {ButtonId::A,      BoardConfig::PIN_BUTTON_A},
    {ButtonId::B,      BoardConfig::PIN_BUTTON_B},
    {ButtonId::C,      BoardConfig::PIN_BUTTON_C},
    {ButtonId::D,      BoardConfig::PIN_BUTTON_D},
    {ButtonId::Select, BoardConfig::PIN_BUTTON_SELECT},
    {ButtonId::Start,  BoardConfig::PIN_BUTTON_START},
};

static_assert(sizeof(BUTTON_PINS) / sizeof(BUTTON_PINS[0]) == BUTTON_COUNT,
              "Every ButtonId needs exactly one pin");

}

ButtonReader::ButtonReader() {
    size_t index = 0;
    for (const ButtonPin& entry : BUTTON_PINS) {
        _channels[index].id = entry.id;
        _channels[index].pin = entry.pin;
        index++;
    }
}

void ButtonReader::begin() {
    for (const Channel& channel : _channels) {
        pinMode(channel.pin, INPUT_PULLUP);
    }
}

void ButtonReader::poll(IButtonEventHandler& handler) {
    const uint32_t now = millis();

    for (Channel& channel : _channels) {
        const bool pressed = digitalRead(channel.pin) == LOW;
        if (!channel.debouncer.update(pressed, now)) continue;

        if (channel.debouncer.isPressed()) {
            handler.onButtonPressed(channel.id);
        } else {
            handler.onButtonReleased(channel.id);
        }
    }
}
