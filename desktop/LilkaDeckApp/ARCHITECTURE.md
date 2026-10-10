# LilkaDeckApp architecture

The desktop application configures a Lilka macropad: it reads and writes profiles (`config.json` and
64x64 RGB565 icons) over a USB serial line. Avalonia 12, .NET 10, Linux and Windows.

## Layers

Dependencies point down. Only the top two layers know about Avalonia.

| Layer | Folders | Knows about |
|---|---|---|
| View | `MainWindow*`, `App*`, `Converters`, `Hosting` (window, tray) | Avalonia, view models |
| Composition | `Composition` | everything, creates the object graph |
| Presentation | `ViewModels`, `Mvvm` | application services, domain |
| Application | `Profiles`, `Sync`, `Services` | domain, device interfaces |
| Device | `Device`, `Protocol`, `Transport` | domain, protocol |
| Domain / format | `Domain`, `Imaging`, `Models` | nothing |

`Imaging/RawIconBitmapLoader` and `Services/AvaloniaFilePicker`, `AvaloniaUiDispatcher` are the only
non-view classes that touch Avalonia; each hides behind an interface or a converter.

## Wiring

`DeckApplication.Create` is the composition root. `MainWindow` creates it with the two platform
pieces it owns (file picker, UI dispatcher), shows `ViewModel`, and keeps only what needs the view:
drag and drop, the shortcut recorder, log scrolling, releasing bitmaps, hiding in the tray.

```
MainViewModel
 |- ActivityViewModel     log and save progress
 |- ConnectionViewModel   status text and flags
 |- ProfileViewModel      open profile: name, color, previous / next / add / delete
 |- DeckViewModel         eight DeckButtonViewModel, selection
 |- ButtonEditorViewModel icon name, action type, action text, media keys
 `- IconImportViewModel   file picker, drop, gallery (IconGalleryViewModel)
```

The view models do not know each other. `DeckApplication` connects them with events
(for example `deck.Selected -> editor.Show`, `editor.Edited -> saver.RequestSave`).

## Editing state

`DeckEditSession` owns the state of the open profile: `DeckState` (eight buttons) and the raw action
text. The raw text is kept apart from the tokens, so a URL with `,` or `+` survives switching the action
type between shortcut and launch. The editor sees it as `IButtonStore` and `IIconImporter`.

Icons live in a cache per profile (`Cache/profile_N/`). Loading a profile always fetches its icons again;
the device is the source of truth. An icon imported by the user is flagged "needs upload" until a save
has sent it.

## Talking to the device

```
SerialPortTransport -> DeviceSession -> LilkaDeviceClient (ILilkaDevice)
        ^                  ^                    ^
 ConnectionMonitor ----- finds the port, pings   |
        `-> DeviceConnection (IDeviceSource, IDeviceEvents) -> DeviceGateway
```

- `DeviceSession` has one reader. Replies go to whoever registered an expectation, which happens before the
  request is sent. Requests are serialized by an exchange lock.
- `DeviceGateway` is what the rest of the application uses (`IProfileDevice`, `IProfileFileSource`,
  `IProfileSyncDevice`). A missing, silent or gone device gives "nothing" (no ids, no file, false); only saving
  throws, so the failure can be reported.
- `ProfileSaver` saves the open profile: `RequestSave` waits 1.5 s for a pause in the edits, `SaveNowAsync`
  saves at once. Changes made during a save cause exactly one more save (`AutoSyncCoordinator`).
- `DeviceEventPresenter` and `FailureReporter` turn connection events and failures into status and log lines.
  `DeviceErrorText` words timeouts for the user; the protocol and device layers contain no UI text.

## Threads

View models are touched on the UI thread only. Device events arrive on background threads and go through
`IUiDispatcher.Post`. `ProfileSaver` and the view models rely on the UI synchronization context for the
continuation after each `await`.

## Testing

The classes without Avalonia (view models, `DeckEditSession`, `ProfileSaver`, `DeviceGateway`,
`DeviceEventPresenter`, parsers, coordinators) can be tested with fake dependencies. Avalonia-bound code
(converters, XAML, file picker) is checked by building and running the application.

## Known limits

- An interrupted sync leaves the firmware waiting for file bytes until its 3 s timeout; a command sent in
  that moment ends up in the file.
- The firmware sends no `ACK_DONE` for an empty file, so the client does not wait for one.
- `KeyCaptureMap` cannot tell NumPad Enter or the right-hand modifiers from their main-keyboard twins.
- Launching on `EXECUTE:` runs whatever the device sends. This is a deliberate decision, not a gap.
- Not tested on Windows.
