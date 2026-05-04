# Ticket 095: Rotate chips

You are working on the Digital Logic Sim Mobile Unity project. This ticket adds **rotation** for chips on the canvas: the user can rotate placed chips in 90° steps (e.g. 0°, 90°, 180°, 270°) for better circuit layout and orientation.

---

## Behaviour

### What the user can do
- **Rotate a chip** (or selected chips) by a fixed step, e.g. **90° clockwise** (or offer both clockwise and anti-clockwise). Rotation is in **90° steps** so the layout stays on-grid and predictable.
- **UI:** Use the **context menu** (right-click on desktop; same action on mobile) as the primary way: e.g. "Rotate 90°" or "Rotate" with sub-options. Optionally add a keyboard shortcut on PC (e.g. R or Ctrl+R) and/or a toolbar button if it fits the existing UI.
- **Selection:** When multiple chips are selected, rotating should rotate **all selected chips** by the same step (each around its own centre).
- **Persistence:** The rotation of each chip **persists with the project** (save/load). After reload, chips appear at their saved rotation.

### What stays the same
- **Wiring:** Wires stay connected to the same pins; rotation is visual/layout only. Pin positions and connection points must be computed from the chip’s position and rotation (and pin layout).
- **Simulation:** Simulation logic is unchanged; rotation does not affect signal flow.
- **Level mode:** If chips in levels have restrictions, rotation should be allowed where it makes sense (same as moving); if there are level-specific rules, follow them.

---

## Implementation notes

### Data model
- **SubChipDescription** (and any equivalent for top-level chips if different) needs a **rotation** value (e.g. degrees: 0, 90, 180, 270). Default 0 for existing saves.
- **SubChipInstance** (and drawing code) must use this rotation when computing position and drawing. Ensure **pin positions** and **hit areas** are computed from the rotated transform so selection, wiring, and interaction remain correct.

### Drawing
- **DevSceneDrawer** (or equivalent) already draws chips at `subchip.Position` and uses `ChipDescription.ShapeRotation` for shape drawing. You will need to apply **instance rotation** (from SubChipDescription / SubChipInstance) when drawing each placed chip and its pins, so the chip and its pins are drawn rotated. Existing `DrawChipShape(..., desc.ShapeRotation, ...)` may be per-description; instance rotation is per-placed-chip and should be applied on top (e.g. rotate around chip centre).

### Save/load
- **DescriptionCreator** (or wherever SubChipDescription is serialized): include the new rotation field with a default of 0 so old projects load without rotation and new saves store it.
- **Upgrade / migration:** If you add a new field, ensure older save files without it default to 0° rotation.

### Context menu
- **ContextMenu.cs:** Add a "Rotate 90°" (or "Rotate" with clockwise/anti-clockwise) entry when a chip or multiple chips are selected. Invoke the rotation logic (e.g. add 90° or -90° to the instance rotation, wrapping at 360°). Reuse the same pattern as other context menu actions (e.g. Edit, Delete).

### Edge cases
- **Custom chips:** When a chip is a custom chip (has subchips), rotating the parent should rotate the whole chip; subchips are drawn relative to the parent, so their positions are already in parent space—apply parent rotation when drawing the parent chip.
- **Pins and wires:** Wire endpoints are at pin positions; after rotation, pin world positions must be computed from chip centre + rotated pin offset so wires still attach correctly.

---

## Success criteria

- User can rotate a placed chip (or multiple selected chips) in 90° steps via context menu (and optionally shortcut/toolbar).
- Rotated chips draw correctly and pins/wires remain correct (wires stay attached, hit-test works).
- Rotation persists with the project (save/load).
- No regressions: movement, deletion, wiring, and simulation behave as before.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Reports/Ticket_095_Rotate_Chips_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.
