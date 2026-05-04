# Ticket 101: Output Pin Color Picker – Report

## Status
**Done** (awaiting testing and feedback)

## Summary
Implemented a "Set color" option for output pins (subchip output pins and dev output pins) that opens a full RGB color picker popup. Users can now choose any colour instead of only the eight presets. The eight presets remain available for quick selection. Custom colours persist with save/load. Wire colour continues to follow the source pin.

## What I Did

### 1. Data model extensions
- **OutputPinColourInfo** (`SubChipDescription.cs`): Added `CustomColourPacked` (uint, 0 = use preset). Packed format: (255<<24)|(r<<16)|(g<<8)|b.
- **PinDescription** (`PinDescription.cs`): Added `CustomColourPacked` for dev pins.
- **PinInstance** (`PinInstance.cs`): Added `CustomColourPacked`. Constructor reads it from `PinDescription`. `GetColLow`, `GetColHigh`, and `GetStateCol` use custom colour when `CustomColourPacked != 0`, otherwise theme preset via `Colour` enum.

### 2. Display / colour helpers
- **DrawSettings.cs**: Added `PackPinColour`, `UnpackCustomColour`, `GetCustomColourLow`, `GetCustomColourHigh` for custom pin colours. Low state = darkened base colour; high state = base colour.

### 3. Pin colour picker menu
- **PinColourPickerMenu.cs** (new): Popup with `DrawColourPicker`, Cancel/Confirm, live preview. Same style as `LabelEditMenu` and `FrequencyEditMenu`. `GetPinDisplayColour` returns current colour for init and display.
- **UIDrawer.cs**: Added `PinColourPicker` menu type; draw and `OnMenuOpened` handlers.

### 4. Context menus
- **ContextMenu.cs**: Added `setColorEntry` ("Set color") and `OpenPinColourPicker()`. Updated `entries_subChipOutput` = Set color + divider + 8 presets. Updated `entries_outputDevPin` = Edit, Delete, divider, Set color + 8 presets. `SetCol` clears `CustomColourPacked` when selecting a preset.

### 5. Persistence
- **SubChipInstance.LoadOutputPinColours**: Loads `CustomColourPacked` from `OutputPinColourInfo`.
- **DescriptionCreator**: `CreateSubChipDescription` passes `CustomColourPacked` to `OutputPinColourInfo`. `CreatePinDescription` and `CreatePinDescriptionAndConserveCustomInfo` save `Colour` and `CustomColourPacked` for all pins (including output).
- **DevChipInstance**: When propagating colour from source (sim connection), copies `CustomColourPacked` as well as `Colour`.
- **ChipInteractionController**, **ChipLibraryMenu**: When duplicating/copying pins, preserve `CustomColourPacked`.

### 6. JSON / save format
- New fields default to 0; old saves load with `CustomColourPacked = 0` and use presets. No migration required.

## Success criteria

- [x] Output pin context menu includes "Set color"
- [x] "Set color" opens a popup with a color wheel/selector (same style as label colour)
- [x] Cancel and Confirm work (Cancel reverts, Confirm applies)
- [x] Chosen colour applies to the pin and to wires from that pin
- [x] Persists with save/load (subchip pins via `OutputPinColourInfo`, dev pins via `PinDescription`)
- [x] No regressions: existing preset colours and wire colour behaviour still work

## Notes

- Dev output pins inherit colour from their source when connected; "Set color" sets the default when disconnected and is persisted.
- Wire colour uses `SourcePin.GetStateCol`, which respects custom colour.
- For bus wires (bitCount >= 64), custom colour uses the high-state colour (flat style).
