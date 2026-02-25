# Ticket 098: Label Chip – Report

## Status
**Done**

## Summary
Implemented the Label chip: a display-only component that shows user-editable text on the canvas. No pins, no simulation logic. Uses the same label look as other chip labels (text in a fitted box with configurable background colour). Default colour matches PinLabelCol. Users can place from BASIC, edit text and colour via context menu ("Edit"), and the label persists with the project.

## What I Did

### 1. Chip type and registration
- **ChipTypes.cs:** Added `ChipType.Label` enum value
- **ChipTypeHelper.cs:** Added `{ ChipType.Label, "LABEL" }` name mapping
- **BuiltinChipCreator.cs:** Added `CreateLabel()` – chip with no pins
- **BuiltinCollectionCreator.cs:** Label chip in BASIC collection

### 2. Drawing
- **DevSceneDrawer.cs:**
  - `DrawLabelChip()` – draws label as fitted box (text + background), same style as DrawSubChipLabel
  - `CalculateLabelChipBoxSize()` – computes box size from text (fitted to content)
  - Default background colour = PinLabelCol; custom colour from InternalData[0]
  - In `DrawAllPinNamesAndChipLabels`, skip `DrawSubChipLabel` for Label chips

### 3. Edit menu and context menu
- **LabelEditMenu.cs (new):** Popup with text input field, colour selector (7 options: default + 6 presets), cancel/confirm. On confirm, sets `subchip.Label` and `InternalData[0]` for colour.
- **UIDrawer.cs:** Added `LabelEdit` menu type; draw and `OnMenuOpened` handlers
- **ContextMenu.cs:** Added `entries_builtinLabelChip` (Edit, Info, Delete) and `OpenLabelEditMenu`; wired Label chip to use these entries in both right-click and centered context menu flows

### 4. Persistence and placement
- **DescriptionCreator.cs:** In `CreateBuiltinSubChipDescriptionForPlacement`, pass `"Label"` as default label text when `type == ChipType.Label`
- Label text persists via existing `SubChipDescription.Label` and `DescriptionCreator.CreateSubChipDescription`, which already saves `subChip.Label`

### 5. Library and simulation
- **DescriptionCreator.cs:** Added `CreateDefaultInstanceData(ChipType.Label)` → `new uint[1]{0}` for colour storage
- **SubChipInstance.cs:** Override `Size` for Label chip – returns computed label box size (fitted to text)
- **Simulator.cs:** Added `case ChipType.Label: break;` in `ProcessBuiltinChip` (no-op; no pins, no logic)
- **ChipLibraryMenu.cs:** Added `DrawBuiltinChipDisplay` branch for Label – label-box style preview

## Success criteria (verified)

- [x] User can place a "Label" chip from the library onto the canvas
- [x] The chip displays user-editable text (no pins, no simulation)
- [x] User can change the text via context menu ("Edit")
- [x] Label text persists with the project (save/load via `SubChipDescription.Label`)
- [x] Label chips can be moved and deleted like other chips
- [x] No regressions – Label is a new chip type, no changes to existing chip logic

## Notes

- Label chip uses `SubChipDescription.Label` for the text; `InternalData[0]` stores colour index (0 = default PinLabelCol)
- Label is **allowed in levels** (not in `IsDisabledInLevels` / `IsSpecialChipDisabledInLevel`)
- Label chip appears in the BASIC library collection alongside NAND, Clock, Pulse, etc.
- Edit menu limits label length to 100 characters
