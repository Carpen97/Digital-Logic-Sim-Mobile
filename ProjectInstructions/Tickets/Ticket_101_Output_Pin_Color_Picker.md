# Ticket 101: Output pin color picker (color wheel / full color selector)

**Type:** Improvement  
**Status:** In Progress

---

## Summary

Currently, when the user opens the context menu on an **output pin** (subchip output or dev output pin), they see a list of **eight preset colors** (Red, Orange, Yellow, Green, Blue, Violet, Pink, White) to choose from. The pin color also controls the **wire color** when connected. This ticket adds a **"Set color"** option that opens a **new popup overlay** where the user can choose the color from a **color wheel or color selector**—the same style used elsewhere in the app (e.g. when setting label background colour in LabelEditMenu). This gives full RGB (and optionally alpha) choice instead of only the eight presets.

---

## Requirements

- In the **output pin context menu** (subchip output pins and dev output pins), add an entry **"Set color"** (or equivalent wording).
- Selecting "Set color" opens a **new popup/overlay** (not the existing preset list). The popup contains a **color wheel or color selector**—reuse the same control used for label colour (e.g. `DrawColourPicker` or equivalent in the codebase) so the UX is consistent.
- User selects a color in the popup and confirms (e.g. Confirm/Cancel). The chosen color is applied to the **output pin** and persists with save/load. **Wire color** continues to follow the pin color (existing behaviour).
- Existing behaviour: the eight preset colours can remain as quick options in the context menu, **or** be replaced by only "Set color" that opens the picker; decide based on UX (either keep presets + add "Set color", or replace with single "Set color" entry). Prefer adding "Set color" so users can still use presets or pick any colour.

---

## Success criteria

- Output pin context menu includes "Set color" (and optionally keeps the eight presets).
- "Set color" opens a popup with a color wheel/selector (same style as label colour picker).
- Chosen color applies to the pin and to wires connected from that pin; persists with save/load.

---

## References

- Kickoff: `ProjectInstructions/WorkerComm/Latest_Kickoffs/Ticket_101_Output_Pin_Color_Picker_Kickoff.md`
- Report (when done): `ProjectInstructions/WorkerComm/Reports/Ticket_101_Output_Pin_Color_Picker_Report.md`
- Existing colour picker: e.g. `LabelEditMenu.cs` uses `Seb.Vis.UI.UI.DrawColourPicker`; pin colour currently uses `PinColour` enum (eight values) and may need extension for arbitrary RGB (e.g. custom colour storage).
