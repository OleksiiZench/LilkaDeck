#include "input/ActionExecutor.h"
#include "input/KeyLookup.h"
#include "sync/SyncProtocol.h"
#include "util/Logger.h"

namespace {

constexpr const char* TAG = "HID";
constexpr const char* LAUNCH_TYPE = "launch";

}

ActionExecutor::ActionExecutor(IHidOutput& hid, ISerialLink& hostLink)
    : _hid(hid), _hostLink(hostLink) {}

void ActionExecutor::execute(const ButtonConfig& button) {
    if (button.type == LAUNCH_TYPE) {
        requestLaunch(button.actions);
    } else {
        sendKeys(button.actions);
    }
}

void ActionExecutor::releaseAll() {
    _hid.releaseAllKeys();
}

// Launching programs is the desktop app's job; the device only forwards the first target.
void ActionExecutor::requestLaunch(const std::vector<String>& actions) {
    if (actions.empty()) return;

    _hostLink.writeLine(String(SyncProtocol::Notification::EXECUTE) + actions[0]);
}

void ActionExecutor::sendKeys(const std::vector<String>& actions) {
    for (const String& action : actions) {
        sendAction(action);
    }
}

void ActionExecutor::sendAction(const String& action) {
    if (isMediaAction(action)) {
        sendMediaKey(action);
    } else {
        sendKeyboardKey(action);
    }
}

void ActionExecutor::sendMediaKey(const String& action) {
    uint16_t usageCode;
    if (!tryLookupMediaKey(action, usageCode)) {
        Log::warn(TAG, "Unmapped media action: %s", action.c_str());
        return;
    }

    Log::info(TAG, "Sending media key: %s", action.c_str());
    _hid.tapMediaKey(usageCode);
}

void ActionExecutor::sendKeyboardKey(const String& action) {
    uint8_t keycode;
    if (!tryLookupKeyboardKey(action, keycode)) {
        Log::warn(TAG, "Unmapped keycode string: %s", action.c_str());
        return;
    }

    _hid.pressKey(keycode);
}
