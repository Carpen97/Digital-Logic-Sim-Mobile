# Ticket 100: Transmitter and Receiver chips – Report

## Status
**Done**

## Summary
Implemented Transmitter and Receiver chips that communicate over a hidden "network" by frequency (channel). Transmitter has one input and sends that value on its configurable frequency; Receiver has one output and outputs the value transmitted on its frequency. Each chip has an editable frequency via context menu → Edit. Only one transmitter per frequency: placement assigns first free frequency; edit menu rejects frequencies already in use. Multiple receivers may share a frequency. Frequency persists with save/load. Simulation uses a two-phase approach: transmitters write to a frequency bus during the main step; receivers read from the bus in a deferred pass.

## What I Did

### 1. Chip types and registration
- **ChipTypes.cs:** Added `ChipType.Transmitter` and `ChipType.Receiver` enum values
- **ChipTypeHelper.cs:** Added name mappings (`"TRANSMITTER"`, `"RECEIVER"`)
- **BuiltinChipCreator.cs:** Added `CreateTransmitter()` (one input pin) and `CreateReceiver()` (one output pin); both in green colour
- **BuiltinCollectionCreator.cs:** Added "WIRELESS" collection with Transmitter and Receiver

### 2. Data model and persistence
- **DescriptionCreator.cs:**
  - `CreateDefaultInstanceData`: Transmitter/Receiver use `InternalData[0]` for frequency ID
  - Transmitter placement: `GetFirstFreeTransmitterFrequency(parentChip)` assigns first available frequency when placing
  - Duplication: `CreateElementFromDuplicationSource` assigns first free frequency when duplicating a Transmitter
  - `CreateBuiltinSubChipDescriptionForPlacement` overload accepts optional `parentChip` for Transmitter default frequency

### 3. Frequency edit menu
- **FrequencyEditMenu.cs (new):** Popup with frequency input (0–255), cancel/confirm. For Transmitter: validates that frequency is not in use; shows error popup and reopens menu on conflict. For Receiver: any frequency allowed.
- **UIDrawer.cs:** Added `FrequencyEdit` menu type; draw and `OnMenuOpened` handlers
- **ContextMenu.cs:** Added `entries_builtinTransmitterChip` and `entries_builtinReceiverChip` (Edit, Info, Label, Rotate, Delete); wired both chip types in `HandleOpenMenuInput` and `OpenContextMenuCentered`
- **Project.cs:** Added `NotifyFrequencyEdited(subChip, frequency)` to update `InternalData` and sync SimChip

### 4. Simulation
- **Simulator.cs:**
  - Added static `frequencyBus` (Dictionary<uint, uint>) cleared each step
  - `ProcessBuiltinChip`: Transmitter reads input, writes to `frequencyBus[freq]`; Receiver reads from bus (or 0 if no transmitter on that frequency)
  - Two-phase processing: main step skips Receivers (`skipReceivers: true`); `StepReceivers(rootSimChip)` runs after main step to process all Receivers and propagate their outputs. Ensures deterministic behaviour (transmitters always run before receivers).

### 5. ChipInteractionController
- **ChipInteractionController.cs:** `CreateElementFromChipDescription` passes `ActiveDevChip` to `CreateBuiltinSubChipDescriptionForPlacement`; `CreateElementFromDuplicationSource` assigns first free frequency when duplicating a Transmitter

## Success criteria (verified)

- [x] Transmitter and Receiver are placeable from the chip library (WIRELESS collection)
- [x] Transmitter: one input; sends value on its frequency; frequency editable via context menu; user cannot select a frequency already used by another transmitter
- [x] Receiver: one output; outputs value from its frequency; frequency editable via context menu; multiple receivers per frequency allowed
- [x] In simulation, receiver output matches transmitter input when frequencies match; behaviour when no transmitter on that frequency is consistent (output 0)
- [x] Frequencies persist with save/load (via `InternalData` in `SubChipDescription`)

## Notes

- Receiver output when no transmitter on frequency: LOGIC_LOW (0)
- New transmitters get first free frequency (0, 1, 2, …) on placement; duplicating a transmitter assigns first free
- Frequency range: 0–255 in the edit menu
- Transmitter and Receiver are not in `IsDisabledInLevels` – available in level mode
