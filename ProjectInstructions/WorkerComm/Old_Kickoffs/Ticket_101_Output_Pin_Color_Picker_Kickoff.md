# Ticket 101: Output pin color picker (color wheel / full color selector)

You are working on the Digital Logic Sim Mobile Unity project. Output pins (subchip output pins and dev chip output pins) currently have a context menu with **eight preset colours** (Red, Orange, Yellow, Green, Blue, Violet, Pink, White). The pin colour also controls the **wire colour** when a wire is connected from that pin. This ticket adds a **"Set color"** option that opens a **new popup overlay** with a **color wheel or color selector**—the same style used for label background colour (e.g. in `LabelEditMenu.cs`, which uses `Seb.Vis.UI.UI.DrawColourPicker`). The user can then choose any colour (full RGB) instead of only the eight presets.

---

## Current behaviour

- **ContextMenu.cs:** Output pins use `entries_subChipOutput` and dev output pins use `entries_outputDevPin`; both include `pinColEntries`, which are one menu entry per `PinColour` enum value (8 presets). Selecting one calls `SetCol(PinColour)` and sets `pin.Colour` (and for subchips, updates `OutputPinColourInfo`).
- **PinColour** enum (in `PinDescription.cs`): Red, Orange, Yellow, Green, Blue, Violet, Pink, White.
- **Wire colour** is derived from the source (output) pin colour elsewhere in the codebase.

---

## What to implement

1. **Add "Set color" to the output pin context menu**
   - For **subchip output pins** (`entries_subChipOutput`): add a menu entry "Set color" (or "Set colour") that opens the new popup instead of setting a preset. You can keep the eight presets as additional quick options, or have only "Set color" that opens the picker; prefer adding "Set color" so both presets and full picker are available.
   - For **dev output pins** (`entries_outputDevPin`): same—add "Set color" that opens the new popup.

2. **New popup: Pin colour picker**
   - Create a new menu/popup (e.g. `PinColourPickerMenu.cs` or extend an existing menu type) that:
     - Is shown as an overlay (fullscreen panel or centred panel, consistent with other edit menus).
     - Contains a **color wheel / color selector**—reuse the same control as in `LabelEditMenu.cs`: `Seb.Vis.UI.UI.DrawColourPicker(ID, position, size, anchor)`. Optionally add an alpha slider if wire/pin colour supports alpha; if not, use full opacity.
     - Has **Cancel** and **Confirm** (or Apply) so the user can confirm the chosen colour or cancel.
     - On confirm: set the pin’s colour to the chosen value. This may require extending the data model: currently pins use `PinColour` enum (8 values). To support arbitrary RGB, either:
       - Add a "custom" or extended representation (e.g. packed RGBA in `OutputPinColourInfo` or on `PinInstance`/`PinDescription`, and a way to map enum presets to RGB for display), or
       - Store RGB for all pins and map the 8 presets to fixed RGB values when loading old saves. Choose an approach that fits the existing save format and drawing code (wires and pins read colour from pin data).
     - When opening the popup, initialise the colour picker to the pin’s **current** colour (if it’s a preset, use the preset’s RGB; if custom, use the stored RGB).

3. **Persistence and wire colour**
   - Ensure the chosen colour is stored so it persists with save/load (e.g. in `OutputPinColourInfo` for subchip pins, or in `PinDescription` for dev pins). Wire colour already follows the source pin; no change needed there if the pin’s colour is updated correctly.

4. **UIDrawer**
   - Register the new menu type (e.g. `PinColourPicker`) and its draw/open handlers so the popup is shown when "Set color" is chosen and input is routed correctly.

---

## Implementation notes

- **LabelEditMenu** and **FrequencyEditMenu** (for Transmitter/Receiver) are good references for a popup with colour picker and Cancel/Confirm.
- **PinInstance.Colour** is currently `PinColour` (enum). Drawing and wire colour use this. To support arbitrary RGB you may need to add a field (e.g. `CustomColourPacked` or store RGB in description) and have the drawing layer use it when present; otherwise fall back to enum→RGB mapping for the 8 presets.
- **OutputPinColourInfo** has `PinColour PinColour` and `int PinID`. It may need an optional or extended format for custom RGB (e.g. packed uint) for save/load; ensure upgrade/migration if you change the format.

---

## Success criteria

- Output pin context menu includes "Set color".
- "Set color" opens a popup with a color wheel/selector (same style as label colour); Cancel and Confirm work.
- Chosen colour applies to the pin and to wires from that pin; persists with save/load.
- No regressions: existing preset colours and wire colour behaviour still work.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Reports/Ticket_101_Output_Pin_Color_Picker_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.
