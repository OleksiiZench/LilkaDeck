#pragma once

#include <Arduino.h>
#include <vector>
#include "config/ConfigManager.h"
#include "input/IHidOutput.h"
#include "sync/ISerialLink.h"

// Carries out what a macro button is configured to do: ask the desktop to launch
// something, or emulate keyboard and media keys over USB.
class ActionExecutor {
public:
    ActionExecutor(IHidOutput& hid, ISerialLink& hostLink);

    void execute(const ButtonConfig& button);
    // Prevents phantom keystrokes on the host after a shortcut has been held.
    void releaseAll();

private:
    IHidOutput& _hid;
    ISerialLink& _hostLink;

    void requestLaunch(const std::vector<String>& actions);
    void sendKeys(const std::vector<String>& actions);
    void sendAction(const String& action);
    void sendMediaKey(const String& action);
    void sendKeyboardKey(const String& action);
};
