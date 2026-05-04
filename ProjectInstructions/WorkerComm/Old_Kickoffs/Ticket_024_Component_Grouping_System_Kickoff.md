# Ticket 024: Component grouping system

You are working on the Digital Logic Sim Mobile Unity project. This ticket adds **groups**: multiple chips tied together so they behave as one unit for selection and movement, with the option to save a group and place it from the library (like a chip).

---

## Behaviour

### Creating a group
- User selects multiple chips on the canvas.
- In the **context menu** (right-click on desktop; same actions available via mobile equivalent), add **"Make group"** (or "Group").
- The selected chips become one group (e.g. three chips → one group of three).

### Selection
- Once grouped, **selecting any one chip in the group selects the entire group**. So one click on any member → whole group selected.

### Movement
- Because the whole group is selected together, moving works like multi-selection: drag once, all chips in the group move together. No extra logic beyond treating the group as the selection.

### Ungroup
- With the group selected, context menu shows **"Ungroup"** (or "Split group"). Choosing it dissolves the group; those chips are again independent.

### Save group
- With a group selected, context menu shows **"Save group"**.
- Flow is **the same as saving a normal chip**: user is prompted to give the group a **name** (required), then the group is saved.
- Saved groups **live in the same library** as chips (same list, searchable by name).
- In the library, show a **marker** (e.g. icon/badge) so the user can tell at a glance that an item is a **group** rather than a chip.

### Placing a saved group
- User picks a saved group from the library (same way as picking a chip).
- **Placing** a saved group **spawns the full set of chips** in their relative positions (and wires between them preserved), as a **group** (click one selects all until they ungroup).
- So: same library UX as chips; the only difference is that placing spawns multiple chips as one group instead of one chip.

---

## Library preview

- The library currently shows a **preview** of the selected item (see `ChipLibraryMenu.cs` – `DrawChipPreview` etc.). For a chip it draws that chip.
- For a **saved group**, we need to **draw a preview of the group**: a miniature view of all chips in the group in their relative layout (and optionally wires). This will require extending or adding preview logic to render a multi-chip “scene” instead of a single chip. Reuse or adapt existing chip preview rendering where possible.

---

## Context menu (and mobile)

- **Group** / **Ungroup** / **Save group** live in the **context menu**.
- On mobile, the same actions must be available (context menu or equivalent entry point).
- Only show "Make group" when multiple chips are selected; only show "Ungroup" / "Save group" when a group (or group member) is selected.

---

## Success criteria

- User can select multiple chips and create a group via context menu.
- Clicking one chip in a group selects the whole group; moving moves all.
- User can ungroup via context menu.
- User can save a group (with name) via context menu; saved groups appear in the library with a group marker and are searchable.
- Library shows a **group preview** (multi-chip mini view) when a saved group is selected.
- Placing a saved group from the library spawns all chips in layout as a group (grouped until user ungroups).
- No regressions: existing chip selection, movement, save chip, and library behaviour unchanged.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Reports/Ticket_024_Component_Grouping_System_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.
