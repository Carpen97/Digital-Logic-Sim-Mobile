# Ticket 100: Transmitter and Receiver chips

You are working on the Digital Logic Sim Mobile Unity project. This ticket adds **two new chips**: **Transmitter** and **Receiver**. They communicate over a hidden "network" by **frequency** (channel)—no visible wires. Transmitter has one input and sends that value on its frequency; Receiver has one output and outputs the value transmitted on its frequency when it matches. Each chip has an editable frequency (context menu → edit). **Only one transmitter per frequency** in the scene: when editing a transmitter’s frequency, the user cannot select a frequency already in use by another transmitter.

---

## Behaviour

### Transmitter
- **One input pin.** The value at that input is "transmitted" on a configurable **frequency** (channel). The network is hidden to the user (in-simulation only; no wires).
- **Context menu** → open an **edit menu** where the user can **change the frequency** (e.g. pick a channel/frequency ID). The user **must not** be allowed to select a frequency that is already used by another transmitter in the current scene. Implement this by: when building the list of selectable frequencies (or validating the choice), exclude frequencies already taken by other transmitters; show only available frequencies or show a clear message if the chosen one is occupied and reject the selection.
- Frequency is stored per chip instance and must **persist** with save/load.

### Receiver
- **One output pin.** The output equals the value being transmitted on the receiver’s configured **frequency** (i.e. the input value of the transmitter on that frequency, if any). If no transmitter is on that frequency, define consistent behaviour (e.g. output 0 or "floating").
- **Context menu** → open the same style of **edit menu** to **change the frequency**. Multiple receivers may use the same frequency (they all receive the same value).
- Frequency persists with save/load.

### Simulation
- During simulation, maintain a mapping: frequency → current value (from the transmitter on that frequency). Each simulation step: (1) read all transmitter inputs and update the value for each transmitter’s frequency; (2) for each receiver, set its output from the value on its frequency. Ensure deterministic behaviour when multiple transmitters are not allowed on the same frequency (so there is at most one value per frequency).

---

## Implementation notes

### New chip types
- Add **Transmitter** and **Receiver** as built-in chip types (similar to other single-pin or simple chips). Register them in the chip library (e.g. under a suitable collection like INPUT/OUTPUT or a new "Wireless" or "Communication" category). Each has one pin: transmitter = input, receiver = output.

### Data model
- Each Transmitter and Receiver instance needs a **frequency** field (e.g. int or string ID). Store it in the chip instance data so it saves/loads with the project. Default frequency for new chips: e.g. 0 or first available; ensure new transmitters don’t conflict if there are existing ones (e.g. assign first free frequency by default).

### Edit menu (frequency)
- Reuse or add a small popup/menu (e.g. similar to constant value or label edit): "Set frequency" with a list or input. For **Transmitter**: when opening the menu, compute which frequencies are already used by other transmitters in the scene; only offer frequencies that are free (or the current one if the user is editing this transmitter). When the user tries to apply a frequency that is now taken (e.g. race condition), reject and show a message. For **Receiver**: any frequency can be chosen (multiple receivers per frequency allowed).

### Simulation
- Wire the transmitter output and receiver input into the simulation. Option A: a global or scene-wide "frequency bus" (e.g. dictionary or array keyed by frequency) that transmitters write and receivers read each step. Option B: integrate into existing sim so that "transmitter on frequency F" and "receiver on frequency F" are treated as a logical connection. Ensure step order: first all transmitters push their input to their frequency, then all receivers read from their frequency.

### Edge cases
- **No transmitter on receiver’s frequency:** Define and implement (e.g. receiver output = 0, or leave unchanged). Document in UI or tooltip if helpful.
- **Duplicate frequency on transmitter (prevented by UI):** Validation in the edit flow must prevent saving a frequency that is already used by another transmitter. If the user duplicates a chip, the duplicate may need a different default frequency so it doesn’t conflict.

---

## Success criteria

- Transmitter and Receiver are placeable from the chip library.
- Transmitter: one input; sends value on its frequency; frequency editable via context menu; user cannot select a frequency already used by another transmitter.
- Receiver: one output; outputs value from its frequency; frequency editable via context menu; multiple receivers per frequency allowed.
- In simulation, receiver output matches transmitter input when frequencies match; behaviour when no transmitter on that frequency is consistent.
- Frequencies persist with save/load.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Reports/Ticket_100_Transmitter_Receiver_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.
