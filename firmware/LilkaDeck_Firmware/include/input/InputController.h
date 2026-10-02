#pragma once

#include "config/ConfigManager.h"
#include "core/IProfileSwitcher.h"
#include "display/IDisplay.h"
#include "input/ActionExecutor.h"
#include "input/ButtonReader.h"

// Reacts to button events: macro buttons run their configured action with visual
// feedback, while Select and Start switch profiles.
class InputController : public IButtonEventHandler {
public:
    InputController(ButtonReader& buttons, ConfigManager& config, IDisplay& display,
                    IProfileSwitcher& profiles, ActionExecutor& actions);

    void begin();
    void update();

    void onButtonPressed(ButtonId button) override;
    void onButtonReleased(ButtonId button) override;

private:
    ButtonReader& _buttons;
    ConfigManager& _config;
    IDisplay& _display;
    IProfileSwitcher& _profiles;
    ActionExecutor& _actions;

    void pressMacroButton(IconPosition position);
    void pressSystemButton(ButtonId button);
};
