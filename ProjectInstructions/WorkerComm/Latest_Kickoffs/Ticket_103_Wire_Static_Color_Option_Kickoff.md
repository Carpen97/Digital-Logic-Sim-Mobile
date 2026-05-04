# Ticket 103: Wire static color option (e.g. for clock wires)

You are working on the Digital Logic Sim Mobile Unity project. Wires are currently drawn with a **colour that reflects the signal** (high/low) from the source pin. When a wire is connected to a **clock** (or any high-frequency signal), this causes the wire to flicker, which users find annoying. This ticket adds an option so that a **selected wire** can be set to **never change colour**—it is always drawn in a **fixed (static) colour** regardless of the signal. The wire still carries the correct logic signal; only the **visual** is static. Primary use case: clock wires.

---

## What the user gets

- **Select a wire** (existing behaviour).
- **Context menu** (or equivalent) includes an option such as **"Static color"**, **"Fixed color"**, or **"Don't show signal"**. When the user enables it:
  - The wire is **drawn in a fixed colour** (e.g. a neutral grey or a theme "inactive" colour) and **does not** update with the simulation signal. No flickering.
- The user can **disable** the option later (same menu) to restore normal signal-based colour.
- The **setting persists** with the project (save/load). Simulation behaviour is **unchanged**—only rendering is affected.

---

## Implementation notes

### Wire selection and context menu

- **ContextMenu.cs** (or equivalent): The wire context menu (`entries_wire`) already exists. Add a new entry (e.g. "Static color" or "Fixed color") that toggles the per-wire flag. If the wire has no stored flag, first use defaults to "dynamic" (current behaviour).
- **WireInstance** (or wherever wire data lives): Add a **per-wire property** such as `UseStaticColor` (bool) and optionally `StaticColor` (if you allow choosing the fixed colour). If `UseStaticColor` is true, the **drawing code** uses the static colour instead of the signal-derived colour.

### Drawing

- **DevSceneDrawer** (or wherever wires are drawn): Wire colour is currently derived from the **source pin's signal state** (and possibly pin colour). When drawing a wire, **check the wire's static-color flag**: if set, use the wire's static colour (or a default neutral colour) instead of the live signal colour. Do not change simulation or connectivity—only the colour passed to the draw call.

### Save/load

- **Wire description / serialization**: Ensure the static-color flag (and optional static colour value) is **saved and loaded** with the project (e.g. in the wire description or wire list). Existing projects without the field should load with dynamic colour (default).

### Defaults

- New wires: dynamic colour (current behaviour). User opts in to static colour per wire.
- Static colour default: a single neutral colour (e.g. grey or theme wire-inactive) is enough for v1; optional later: let user pick the static colour.

---

## Success criteria

- User can select a wire and enable "Static color" (or equivalent) from the context menu.
- While enabled, the wire is drawn in a fixed colour and does not change with the signal (no flicker on clock wires).
- User can disable the option to restore normal behaviour.
- Setting persists with save/load; simulation is unchanged.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Reports/Ticket_103_Wire_Static_Color_Option_Report.md`** with Status, Summary, and what you did (see that folder's README for the report template). The PM will use that to update the plan and close the ticket.
