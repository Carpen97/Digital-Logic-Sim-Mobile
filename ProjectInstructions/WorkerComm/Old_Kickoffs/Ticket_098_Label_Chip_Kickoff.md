# Ticket 098: Label chip (text-only display component)

You are working on the Digital Logic Sim Mobile Unity project. This ticket adds a **label chip**: a component that is purely for displaying user-editable text on the canvas. No inputs, no outputs, no simulation—just a placeable text label for annotations, section labels, and circuit documentation.

**Distinct from:** TextDisplay (address-driven 256 strings); button chip labels (labels on the button component).

---

## Behaviour

### What it is
- A **chip** (or chip-like component) that the user can place on the canvas like any other chip.
- It displays **a single user-editable text string** (the “label”).
- It has **no pins**, **no simulation logic**, and **no electrical behaviour**. It is display-only.

### Placing
- User can place it from the chip library (or a dedicated “Labels” / “Annotations” entry). It appears in the library like other chips; consider a clear name (e.g. “Label” or “Text label”) and possibly a category.

### Editing the text
- When the label chip is selected, the user can **edit the label text** via the **context menu** (right-click on desktop; same action available on mobile, e.g. long-press or context menu). For example: “Edit label” or “Set text” that opens a text field or small popup. The user types the desired text; on confirm, the chip displays that text.
- Default text when first placed can be something like “Label” or empty; implementer’s choice.

### Display
- The chip’s visual is **the text itself** (and optionally a simple background/border so it’s visible on the grid). Text should be readable (font size, contrast). Consider word wrap or max width for long text; truncation with ellipsis is acceptable if needed.

### Persistence
- The label text **persists with the project** (save/load). When the project is saved and reopened, label chips show the same text.

### Deletion / movement
- Behaves like other chips: can be selected, moved, deleted (e.g. delete key, eraser, context menu). No special rules.

---

## Implementation notes

- **Chip type:** Add a new built-in chip type (e.g. `ChipType.Label` or `ChipType.TextLabel`) unless the codebase has a better pattern for “non-simulated display only” components. Follow how other simple built-ins (e.g. Constant, or a display-only chip) are registered: `ChipDescription`, `BuiltinChipCreator`, chip library, drawing in `DevSceneDrawer`, save/load of instance data (the text string).
- **Instance data:** Each placed label chip stores its text (e.g. on the `DevChipInstance` or subchip equivalent; use existing persistence patterns such as `InternalData` or a dedicated field that gets serialized with the chip).
- **Context menu:** Add an “Edit label” (or “Set text”) entry when a label chip is selected; it should open a popup or inline editor for the text. Reuse or mirror patterns from existing text-editing flows (e.g. Constant chip value, ROM editor, or chip name edit).
- **Drawing:** In `DevSceneDrawer` (or equivalent), draw the label chip as its text (and optional background). Reuse existing text-drawing and layout helpers used for other chips.
- **Library:** Ensure the label chip appears in the chip library and is placeable. No need for a library “preview” of the text content beyond a simple “Label” or icon if that matches other chips.

---

## Success criteria

- User can place a “label” chip from the library onto the canvas.
- The chip displays user-editable text (no pins, no simulation).
- User can change the text via context menu (e.g. “Edit label” / “Set text”).
- Label text persists with the project (save/load).
- Label chips can be moved and deleted like other chips.
- No regressions to existing chips or simulation.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Ticket_098_Label_Chip_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.
