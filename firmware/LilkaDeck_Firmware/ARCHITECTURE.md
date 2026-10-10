# LilkaDeck Firmware Architecture

Arduino framework on ESP32-S3, built with PlatformIO. The code is C++11
(the Arduino core for this platform version does not enable C++14).

## Layers

| Folder | Contents |
|---|---|
| `app` | `Application`: composition root and boot sequence |
| `input` | `ButtonReader`, `Debouncer`, `InputController`, `ActionExecutor`, `KeyLookup`, `UsbHidOutput` |
| `sync` | `SyncManager`, `SyncCommand`, `FileReceiver`, `ArduinoSerialLink`, `SyncProtocol` |
| `core` | `ProfileManager`, `ProfileNavigator`, `ProfilePresenter` |
| `display` | `IDisplay`, `TftDisplay`, `IconGridLayout` |
| `storage` | `IFileSystem`, `SdFileSystem`, `ProfileRepository` |
| `config` | `BoardConfig`, `ConfigManager` |
| `domain` | `IconPosition`, `ColorUtils` |
| `util` | `Logger` |

## Dependency rule

Logic depends on interfaces (`IDisplay`, `IFileSystem`, `ISerialLink`, `IHidOutput`,
`IProfileSwitcher`), never on concrete hardware classes. Concrete classes are
created in one place only: `Application`.

## Things that are easy to break

- **Shared SPI bus.** The display and the SD card share one bus, and `TFT_eSPI` owns
  the `SPIClass` instance. `SdFileSystem` releases the SD chip select after every
  operation, otherwise the display driver deadlocks on its next draw.
- **Serial is also the protocol channel.** The desktop app parses every line the
  device prints. Never log while streaming a file to the host (`FILE_GET`): the host
  reads raw bytes at that point. Log lines must not start with `EXECUTE:`, `ACK_`,
  `LILKA_PONG` or `PROFILES:`.
- **Protocol constants** live in `sync/SyncProtocol.h` and must match
  `DeviceSession` and `LilkaDeviceClient` in the desktop app.
- **HID registration order.** Keyboard and consumer-control devices register in
  construction order, which is why `_keyboard` is the first member of `Application`.

## Common changes

- New key name: add a row to `KEYBOARD_KEYS` or `MEDIA_KEYS` in `input/KeyLookup.cpp`.
- New host command: add a `SyncCommandType`, parse it in `sync/SyncCommand.cpp`,
  handle it in `SyncManager::dispatch`, and add any reply text to `SyncProtocol.h`.
- New board pin: add it to `config/BoardConfig.h`; pins used by `TFT_eSPI` stay in
  `platformio.ini` build flags.
  