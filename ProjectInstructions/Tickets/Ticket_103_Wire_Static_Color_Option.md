# Ticket 103: Wire static color option (e.g. for clock wires)

**Type:** Improvement  
**Status:** In Progress

---

## Summary

When a wire is connected to a fast-changing signal (e.g. the **clock**), the wire colour updates every simulation step and can be visually distracting or annoying at high frequency. This ticket adds a way to **select a wire** and set it so its **colour never changes** with the signal—it stays a fixed (static) colour regardless of input. Primary use case: clock wires, so the user doesn't have to watch them flicker. The wire still carries the correct signal; only the **drawn colour** is fixed.

---

## Requirements

- User can **select a wire** (existing interaction).
- There is a way to **turn on "static color"** for that wire (e.g. context menu entry like "Static color" or "Fixed color" / "Don't show signal"). When enabled, the wire is drawn in a **fixed colour** (e.g. a neutral or theme colour) and does **not** change with the signal state (high/low).
- The wire still **simulates correctly**; only the visual representation is static. Toggling the option off restores normal signal-based colour.
- The setting **persists** with save/load (per wire).
- Optional: allow choosing the static colour (e.g. grey, or same as current theme for "inactive" wires); otherwise use a single sensible default.

---

## Success criteria

- User can select a wire and enable "static color" (or equivalent).
- While enabled, the wire is drawn in a fixed colour and does not flicker with the signal.
- Setting persists with the project; simulation behaviour is unchanged.

---

## References

- Kickoff: `ProjectInstructions/WorkerComm/Latest_Kickoffs/Ticket_103_Wire_Static_Color_Option_Kickoff.md`
- Report (when done): `ProjectInstructions/WorkerComm/Reports/Ticket_103_Wire_Static_Color_Option_Report.md`
