# Ticket 099: User-created levels

**Type:** Feature  
**Status:** In Progress

---

## Summary

Allow users to create their own levels from a custom chip: the creator designs a chip with inputs and outputs, then uses a "Make level" (or similar) flow to record the level. What is stored and what other players see is **only the input and output pins** (definitions + positions) plus a list of **test cases** (input → steps → expected output). Players get a canvas with those I/O nodes only and must build a circuit that matches the test cases. Levels are stored in a new **level library** (My levels) and played locally. Sharing levels with other players (e.g. via project-sharing-style system) is **out of scope** for this ticket and will be a follow-up.

---

## Requirements

### Create level from chip

- Creator builds a **normal custom chip** in the editor (inputs, outputs, behaviour).
- A new action **"Make level"** (or similar) is available (e.g. from a selected chip in the scene or from the chip library). Triggering it starts the level-creation flow.
- The level is defined by:
  - **I/O only:** Input and output pin definitions (count, names, types) and **positions** (where they appear on the level canvas). The creator’s internal circuit is **not** stored or shown to players.
  - **Test cases:** A list of (input state, clock steps, expected output). Many test cases per level.

### Recording test cases

- **Two ways to populate test cases:**
  1. **Run all combinations:** Option to auto-generate all input combinations (e.g. 2^N for N binary inputs), run the sim with 0 clock steps, and record each output. Include a sensible cap and/or warning for large N (e.g. max combinations or max test cases).
  2. **Manual recording:** Creator sets inputs (and optionally steps the clock one or more times), then hits "Record" to add one test case. Repeat. Flow: set inputs → optional "Step" (clock tick) → "Record" → next test case. Allow reorder/delete of test cases in the list.
- **Combinational vs sequential:** Each test case is (input state, number of clock steps, expected output). 0 steps = combinational; 1+ steps = sequential. Recording UI must allow "Step" (tick clock) so the creator can advance the sim before recording.

### Level library

- **New storage and UI** for user-created levels (e.g. "My levels" or "Level library"): create, list, open, play, delete. Levels live in a dedicated place (separate from chip library and project list). Level has a **name** (required) and optional description; saved with the level.

### Playing a user-created level

- When a player opens a user-created level, they see **only the input and output nodes** at the recorded positions—no chip body, no creator circuit. They build their own circuit; on submit, validation runs all test cases (same idea as existing levels). Reuse existing level validation machinery where possible; extend to support the new level format (I/O + test case list).

---

## Out of scope (this ticket)

- **Sharing levels** with other players (upload/download, project-sharing-style). To be a follow-up after local create/play is tested.

---

## Success criteria

- Creator can start "Make level" from a chip and name the level.
- Creator can record many test cases (manual and/or "run all combinations").
- Recording supports optional clock steps per test case (combinational and sequential).
- Level is stored in the new level library; only I/O definitions + positions + test cases are saved.
- Player can open a user-created level and see only the I/O nodes; validation passes when their circuit matches all test cases.
- No exposure of the creator’s circuit to the player.

---

## References

- Kickoff: `ProjectInstructions/WorkerComm/Latest_Kickoffs/Ticket_099_User_Created_Levels_Kickoff.md`
- Report (when done): `ProjectInstructions/WorkerComm/Reports/Ticket_099_User_Created_Levels_Report.md`
