# Ticket 096: Button chip label

You are working on the Digital Logic Sim Mobile Unity project. Your task is to add a **user-configurable label** to the **button chip** (the pressable input component). The label must be **visible in-game** in both of these cases:
1. **Top-level:** When the button is placed on the main canvas.
2. **Subchip:** When the button is inside a custom chip (viewing the parent chip), the label must still be visible on or next to the button.

## Context

- **Chip:** The built-in **button chip** (`ChipType.Button`). It is an input that outputs high while pressed. It appears in the chip library and can be placed on the canvas or as a sub-component inside custom chips.
- **Drawing:** Button is drawn in `DevSceneDrawer.cs`: `DrawInteractable_Button` for the interactable (clickable) display. When the button is a **subchip**, it is drawn via the subchip display path (e.g. `DrawSubchipDisplays` → same or similar drawing). Any label must be rendered in **both** code paths so the label is visible on the main canvas and when the button is inside another chip.
- **Similar patterns:** Custom chips have name/label handling (e.g. `ChipLabelMenu`, name display location). The Key chip has rebindable key. Consider how other built-in chips store per-instance data (e.g. colour, constant value) for persistence.

## What to do

1. **Data model**
   - Add a way to store a **label string** for a button chip instance (e.g. in the chip description, component data, or equivalent so it saves/loads with the project). Default can be empty or a placeholder like "Button".

2. **UI to set the label**
   - Provide a way for the user to **edit** the button’s label (e.g. context menu entry "Set label" / "Edit label" when a button or button subchip is selected, opening a text field or small menu). Reuse or mirror patterns from existing edit menus (e.g. Constant, Pulse, Key rebind, or chip name/label).

3. **Rendering**
   - **Top-level:** In `DrawInteractable_Button` (or the code path that draws a single button on the main canvas), draw the label text on or beside the button when a label is set. Keep the existing button look and interaction; add the label so it is readable and doesn’t overlap the button badly.
   - **Subchip:** In the subchip drawing path that draws a button **inside** a custom chip, draw the same label so that when the user is viewing the parent chip, each button subchip shows its label. Reuse the same drawing logic or a shared helper if possible.

4. **Persistence**
   - Ensure the label is saved and loaded with the project (and with the chip when it’s a subchip). No regression to existing save/load.

## Success criteria

- User can set a label on a button chip (top-level or as subchip) via in-game UI.
- The label is visible when the button is on the main canvas.
- The label is visible when the button is a subchip (inside a custom chip).
- Labels persist across save/load.
- No regressions: button behaviour, subchip display, and other chips unchanged.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Ticket_096_Button_Chip_Label_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.
