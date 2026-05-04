# Ticket 100: Transmitter and Receiver chips

**Type:** Feature  
**Status:** In Progress

---

## Summary

Add two new built-in chips that communicate over a hidden "network" by frequency (channel). **Transmitter**: one input pin; sends that value on a chosen frequency. **Receiver**: one output pin; outputs the value being transmitted on the same frequency. No visible wires between them. Each chip has an editable **frequency** (context menu → edit). Only one transmitter may use a given frequency per scene; when editing, the user cannot select a frequency already in use by another transmitter.

---

## Requirements

### Transmitter chip
- One **input** pin.
- Takes the input value and transmits it on a **frequency** (channel). The "network" is hidden to the user (in-simulation only; no visible connection).
- **Frequency** is configurable via context menu → open **edit menu**; user can change the frequency.
- **Constraint:** At most one transmitter per frequency in the scene. When editing a transmitter’s frequency, the user **cannot** select a frequency that is already used by another transmitter in the same project/scene. Show which frequencies are occupied (e.g. grey out or hide them, or show a message) so the user can pick only an available one.

### Receiver chip
- One **output** pin.
- Outputs the value currently being transmitted on its configured **frequency**. If a transmitter on that frequency exists, receiver output = transmitter input; otherwise define behaviour (e.g. 0 or undefined).
- **Frequency** is configurable via context menu → open **edit menu**; user can change the frequency. Multiple receivers may share the same frequency (they all get the same transmitted value).

### Edit menu (both chips)
- Context menu entry (e.g. "Edit" or "Set frequency") opens a small menu/popup where the user can **set the frequency** (e.g. pick from a list, or numeric/channel ID). For **transmitter only**: the list (or input) must not allow choosing a frequency that is already used by another transmitter in the scene. Persist frequency with the chip instance (save/load).

---

## Success criteria

- Transmitter and Receiver exist as placeable chips (e.g. in chip library / appropriate collection).
- Transmitter: one input; sends value on its frequency; frequency editable via context menu; cannot select an already-used frequency.
- Receiver: one output; outputs value from its frequency; frequency editable via context menu; multiple receivers per frequency allowed.
- Simulation: receiver output matches transmitter input when frequencies match; behaviour when no transmitter on that frequency is defined and consistent.
- Frequency persists with save/load.

---

## References

- Kickoff: `ProjectInstructions/WorkerComm/Latest_Kickoffs/Ticket_100_Transmitter_Receiver_Kickoff.md`
- Report (when done): `ProjectInstructions/WorkerComm/Reports/Ticket_100_Transmitter_Receiver_Report.md`
