# Ticket 096: Button chip label – Report

## Status

**Done**

## Summary

Implemented user-configurable labels for the button chip (built-in pressable input) with configurable position. Labels are visible when the button is on the main canvas and when it is a subchip inside a custom chip. Label position (X and Y offset) is fully adjustable via sliders and preset buttons, and persists across save/load. Button interaction was improved so right-click opens the context menu from anywhere, and when selected, the chip can be dragged from anywhere including the button centre.

## What I did

### 1. Label rendering (original scope)
- Updated `DrawInteractable_Button` in `DevSceneDrawer.cs` to draw labels with position from `LabelOffset`.
- Added `ResolveButtonLabelAndOffsetForNestedDisplay` to resolve label and offset for nested buttons.
- `DrawSubChipLabel` is skipped for Button chips; labels drawn only in `DrawInteractable_Button`.

### 2. Label position (LabelOffset)
- **Data model:** `LabelOffset` (`Vector2`) in `SubChipDescription` and `SubChipInstance`, default `(0, 1)` (centred below).
- **UI:** ChipLabelMenu sliders for X and Y offset (range -1 to 1, 0 = centre).
- **Persistence:** `DescriptionCreator.CreateSubChipDescription` saves `LabelOffset`. Added `[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]` so `(0, 0)` (centre) is always serialized. Removed migration that incorrectly reset centre to bottom on load.

### 3. ChipLabelMenu UI
- Doubled vertical spacing (rowHeight 2.2→4.4, spacing 0.8→1.6).
- Snap checkbox: constrains sliders to -1, 0, 1. Display: `[ ] Snap` / `[X] Snap`; only "Snap" is the clickable button.
- Preset buttons: **bottom**, **centre**, **top** – set Y instantly and reset X to 0.
- Slider colours changed from white/yellow to grey shades to match UI theme.

### 4. Button interaction
- **Right-click anywhere:** Context menu opens from button centre or outer area. When over display (centre), `DisplayInstance` is resolved to parent `SubChipInstance` for the menu; added `DevChipInstance.TryGetSubChipOwningDisplay`.
- **Outer = select/move, Centre = press:** Element-under-mouse logic: centre notifies `DisplayInstance`, outer notifies `SubChipInstance` from `DrawSubChip`.
- **Selected + click anywhere = drag:** When button is selected, any click (including centre) starts dragging; button press/toggle is skipped via `chipIsSelected` parameter in `DrawInteractable_Button`, `DrawInteractable_Toggle`, `DrawInteractable_RGBTouch`.
- **CameraController:** Updated `isTouchingClickableDisplay` to handle both `DisplayInstance` and `SubChipInstance` for panning prevention.

### 5. Files touched
- `SubChipDescription.cs` – LabelOffset, JsonProperty
- `SubChipInstance.cs` – LabelOffset, removed migration
- `DescriptionCreator.cs` – CreateSubChipDescription passes LabelOffset
- `DevSceneDrawer.cs` – DrawInteractable_Button/Toggle/RGBTouch label/offset, chipIsSelected, DrawClickableDisplay
- `ChipLabelMenu.cs` – Sliders, snap checkbox, preset buttons
- `ContextMenu.cs` – DisplayInstance → SubChipInstance resolution
- `DevChipInstance.cs` – TryGetSubChipOwningDisplay
- `ChipInteractionController.cs` – (no special-case skip; outer/centre logic handles select vs press)
- `CameraController.cs` – isTouchingClickableDisplay for SubChipInstance
- `UI.cs` (Seb) – DrawSlider colours

## Success criteria (verified)

- [x] User can set a label on a button chip (top-level or as subchip) via the LABEL context menu.
- [x] Label is visible when the button is on the main canvas.
- [x] Label is visible when the button is a subchip inside a custom chip.
- [x] Label position (X, Y offset) is configurable and persists across save/load.
- [x] Right-click anywhere on button opens context menu.
- [x] When selected, button can be dragged from anywhere (including centre).
- [x] No regressions: button press, select, move, and other chips unchanged.

## What's left

Nothing. Ticket complete.
