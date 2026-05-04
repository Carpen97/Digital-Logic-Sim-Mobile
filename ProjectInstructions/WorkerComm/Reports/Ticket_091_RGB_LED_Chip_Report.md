# Ticket 091 Report: RGB LED chip

- **Status:** Done
- **Summary:** The RGB LED built-in chip is implemented. It has three 8-bit inputs (R, G, B), no outputs, and an on-chip light that shows the colour from those inputs and updates in real time. It is integrated with the built-in chip set, simulator, and scene rendering, and appears in the chip library under DISPLAY.

## What I did

- **Chip type and naming**
  - Added `DisplayRGBLED` to `ChipType` enum in `Assets/Scripts/Description/Types/SubTypes/ChipTypes.cs`.
  - Added display name `"RGB LED"` in `ChipTypeHelper` in `Assets/Scripts/Description/Helpers/ChipTypeHelper.cs`.

- **Built-in chip creation**
  - In `BuiltinChipCreator.cs`: added `CreateDisplayRGBLED()` with three 8-bit input pins (R, G, B), one display, no output pins; added call to it in `CreateAllBuiltinChipDescriptions`.

- **Simulation**
  - In `SimChip.cs`: for `DisplayRGBLED`, allocated `InternalState` of length 1 to hold packed RGB for the renderer.
  - In `Simulator.cs`: added `ProcessBuiltinChip` case for `DisplayRGBLED` that reads R, G, B from input pins (8-bit, masked with `0xFF`), packs them into `InternalState[0]` as R | (G<<8) | (B<<16), and does not drive any outputs.

- **Rendering**
  - In `DevSceneDrawer.cs`: added branch in `DrawDisplay` for `DisplayRGBLED` that unpacks `InternalState[0]` to a Unity `Color` (R,G,B in 0–1) and reuses `DrawDisplay_LED` for the on-chip light.

- **Chip library and UI**
  - **BuiltinCollectionCreator.cs:** Added `DisplayRGBLED` to the DISPLAY collection.
  - **BottomBarUI.cs:** Included `DisplayRGBLED` in the special-chip list so it is hidden in level mode where other display chips are hidden.
  - **ChipInteractionController.cs:** Included `DisplayRGBLED` in the special-chip list for placement/level rules.
  - **ChipLibraryMenu.cs:** Added `DrawBuiltinChipDisplay` branch for `DisplayRGBLED` (white LED preview via new `UI_DrawLEDDisplayWithColor`); included `DisplayRGBLED` in the "simple display" scale logic for library preview.

- **Persistence and descriptions**
  - **DescriptionCreator.cs:** Added `CreateDefaultInstanceData` case for `DisplayRGBLED` returning one uint (for consistency; runtime colour is driven by inputs).
  - **ChipDescriptionData.cs:** Added library description text for the RGB LED chip.

- **Report**
  - Wrote this report to `ProjectInstructions/WorkerComm/Ticket_091_RGB_LED_Chip_Report.md`.

## What's left

- Nothing. The RGB LED chip can be placed from the chip library, wired to R/G/B sources, and the on-chip light updates in real time. No regressions to existing chips or simulation were introduced; only additive changes and existing patterns (e.g. DisplayLED, DisplayRGB) were followed.
