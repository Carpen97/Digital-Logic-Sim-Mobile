# Ticket 095: Rotate Chips – Report

## Status
**Done**

## Summary
Implemented chip rotation for placed components on the canvas. Users can rotate chips in 90° steps (0°, 90°, 180°, 270°) via context menu ("Rotate 90° CW" / "Rotate 90° CCW"), keyboard shortcuts (E = clockwise, Q = counter-clockwise), or Ctrl + scroll wheel when chips are selected. Rotation persists with the project (save/load), and wires stay connected correctly.

## What I Did

### 1. Data model
- **SubChipDescription.cs:** Added `Rotation` field (int, 0/90/180/270) with `[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]` for backward compatibility (old saves default to 0°)
- **SubChipInstance.cs:** Added `Rotation` property, initialized from `InitialSubChipDesc.Rotation` in constructor; added `RotateBy(int deltaDegrees)` method to change rotation with wrapping
- **SubChipInstance.CreateBoundingBox:** Updated to use AABB that encompasses rotated chip (swap width/height when rotation is 90° or 270°)

### 2. Drawing
- **DevSceneDrawer.cs:**
  - Added `DrawRotatedQuad()` for drawing rotated rectangles (using 4 triangles from centre)
  - Updated `DrawChipShape` Rectangle case to support rotation
  - `DrawSubChip`: Pass total rotation (`desc.ShapeRotation + subchip.Rotation`) to shape drawing
  - Added `MouseInsideRotatedBounds_World()` for correct hit-test on rotated chips (transforms mouse to chip-local space)
  - `DrawLabelChip`: Draw rotated quad when `subchip.Rotation != 0`, use rotated hit test
  - `DrawSubchipDisplays`: Pass rotation to `DrawDisplayWithBackground`; use AABB for mask when chip is rotated; rotate display positions in `DrawDisplay` and `DrawClickableDisplay`

### 3. Pin positions and wire connections
- **PinInstance.cs:**
  - `GetWorldPos()`: Apply instance rotation to local pin offset before adding to chip centre (for both rectangle and custom shapes)
  - `FacingDir`: Rotate base facing direction by instance rotation
  - Added `RotateVector()` helper
  - Removed debug `Debug.Log` from custom-shape pin path

### 4. Save/load
- **DescriptionCreator.cs:** `CreateSubChipDescription` includes `subChip.Rotation`; `SubChipDescription` constructor has optional `rotation` parameter (default 0)
- **RomEditMenu.cs:** Preserve `romChip.Rotation` when changing ROM chip type

### 5. Context menu
- **ContextMenu.cs:** Added "Rotate 90° CW" and "Rotate 90° CCW" entries to all subchip menus (custom, builtin, bus, key, ROM, TextDisplay, Pulse, Constant, Label, LED, Button). Wired to `RotateSelected(±90)` via `CanRotate` / `RotateSelected`.

### 6. PC controls
- **ChipInteractionController.cs:**
  - `RotateSelected(int deltaDegrees)`: Rotates all selected SubChipInstances, records undo
  - `HasRotatableSelection`: True when selection contains at least one SubChipInstance
  - **Q key** (plain): Rotate selected -90° (counter-clockwise)
  - **E key** (plain): Rotate selected +90° (clockwise)
  - **Ctrl + scroll wheel:** Rotate selected based on scroll direction (scroll up = +90°, scroll down = -90°)
- **CameraController.cs:** `CanMiddleMouseZoom` returns false when Ctrl held and `HasRotatableSelection`, so scroll is used for rotation instead of zoom
- **UndoController.cs:** Added `RecordRotateElements` and `RotateUndoAction` for undo/redo of rotation

## Success criteria (verified)

- [x] User can rotate a placed chip (or multiple selected chips) in 90° steps via context menu
- [x] User can rotate via Q/E keys and Ctrl+scroll wheel (PC)
- [x] Rotated chips draw correctly; pins and wires remain correct (wires stay attached, hit-test works)
- [x] Rotation persists with the project (save/load)
- [x] No regressions: movement, deletion, wiring, and simulation behave as before

## Notes

- Q and E (unmodified) were confirmed free—Ctrl+Q is Quit, Ctrl+E is Levels
- Mobile/context-menu flow: Rotation entries appear in all subchip context menus; mobile users can rotate via wrench-tool context menu
- Display components (LED, 7-seg, Button, etc.) rotate with the chip; display positions are transformed by instance rotation
