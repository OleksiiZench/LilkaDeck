#include "input/InputController.h"
#include "input/ButtonLayout.h"
#include "util/Logger.h"

namespace {
constexpr const char* TAG = "INPUT";
}

InputController::InputController(ButtonReader& buttons, ConfigManager& config, IDisplay& display,
                                 IProfileSwitcher& profiles, ActionExecutor& actions)
    : _buttons(buttons), _config(config), _display(display), _profiles(profiles), _actions(actions) {}

void InputController::begin() {
    _buttons.begin();
}

void InputController::update() {
    _buttons.poll(*this);
}

void InputController::onButtonPressed(ButtonId button) {
    IconPosition position;
    if (tryGetIconPosition(button, position)) {
        pressMacroButton(position);
    } else {
        pressSystemButton(button);
    }
}

void InputController::onButtonReleased(ButtonId button) {
    Log::info(TAG, "Released button %d", static_cast<int>(button));

    IconPosition position;
    if (tryGetIconPosition(button, position)) {
        _display.setIconPressed(position, false, _config.getActiveColor());
    }

    // Releasing any button clears the whole HID report, which only ever holds keys from a shortcut.
    _actions.releaseAll();
}

void InputController::pressMacroButton(IconPosition position) {
    Log::info(TAG, "Macro button pressed at position %d", static_cast<int>(position));
    _display.setIconPressed(position, true, _config.getActiveColor());

    const auto& buttons = _config.getButtons();
    auto configured = buttons.find(position);
    if (configured != buttons.end()) {
        _actions.execute(configured->second);
    }
}

void InputController::pressSystemButton(ButtonId button) {
    if (button == ButtonId::Select) {
        Log::info(TAG, "Select -> previous profile");
        _profiles.previousProfile();
    } else if (button == ButtonId::Start) {
        Log::info(TAG, "Start -> next profile");
        _profiles.nextProfile();
    }
}
