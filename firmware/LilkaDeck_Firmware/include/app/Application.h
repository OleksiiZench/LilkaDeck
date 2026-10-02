#pragma once

#include <USBHIDKeyboard.h>
#include "config/ConfigManager.h"
#include "core/ProfileManager.h"
#include "core/ProfileNavigator.h"
#include "core/ProfilePresenter.h"
#include "display/TftDisplay.h"
#include "input/ActionExecutor.h"
#include "input/ButtonReader.h"
#include "input/InputController.h"
#include "input/UsbHidOutput.h"
#include "storage/ProfileRepository.h"
#include "storage/SdFileSystem.h"
#include "sync/ArduinoSerialLink.h"
#include "sync/FileReceiver.h"
#include "sync/SyncManager.h"

// Composition root: creates every component, wires their dependencies and runs the boot sequence.
class Application {
public:
    Application() = default;
    // Components hold references to each other, so a copy would point into the original.
    Application(const Application&) = delete;
    Application& operator=(const Application&) = delete;

    void setup();
    void loop();

private:
    // Members are constructed in declaration order, so each one comes after what it depends on.
    // HID devices also register in construction order, which keeps the keyboard first.
    USBHIDKeyboard _keyboard;
    ConfigManager _config;
    TftDisplay _display;
    SdFileSystem _fileSystem;
    ProfileRepository _repository{_fileSystem};
    ProfileNavigator _navigator;
    ProfilePresenter _presenter{_config, _repository, _display};
    ProfileManager _profiles{_config, _repository, _navigator, _presenter};

    ArduinoSerialLink _serialLink;
    FileReceiver _fileReceiver{_serialLink};
    SyncManager _sync{_serialLink, _repository, _profiles, _fileReceiver};

    UsbHidOutput _hid{_keyboard};
    ActionExecutor _actions{_hid, _serialLink};
    ButtonReader _buttons;
    InputController _input{_buttons, _config, _display, _profiles, _actions};

    void driveDisplayBlkLow();
    void deselectSdCard();
    void startUsb();
    void startDisplay();
    void startProfiles();
    void startInput();
};
