# Ticket 099: User-created levels

You are working on the Digital Logic Sim Mobile Unity project. This ticket adds **user-created levels**: a creator designs a custom chip, then uses a "Make level" flow to turn it into a level. Only the **input and output pins** (definitions + positions) and a list of **test cases** are stored. Other players see just those I/O nodes and must build a circuit that matches. Levels live in a new **level library** (My levels). Sharing levels with other players is **out of scope** (follow-up later).

---

## What the user gets

### Creator flow
1. Build a normal custom chip (inputs, outputs, behaviour) in the editor.
2. Trigger **"Make level"** (or similar)—e.g. from selected chip or chip library.
3. Name the level (required); optional description.
4. **Record test cases** in one of two ways:
   - **Run all combinations:** Auto-generate all input combinations, run sim (0 steps), record each output. Cap/warn for large N.
   - **Manual:** Set inputs → optional "Step" (clock tick) → "Record" to add (input, steps, output). Repeat. Support reorder/delete test cases.
5. Save level to the **level library**.

### Player flow
- Open a user-created level from the level library. See **only** the input and output nodes at the positions the creator defined—no chip body, no creator circuit.
- Build a circuit that, for every test case, produces the expected output. Submit; validation runs all test cases (reuse/adapt existing level validation).

### Data stored per level
- Input/output pin definitions (count, names, types) and **positions** on the canvas.
- List of test cases: each is (input state, number of clock steps, expected output). 0 steps = combinational; 1+ = sequential.
- Level name and optional description.

---

## Implementation notes

### Level library
- New storage location and index for "My levels" (similar in spirit to projects/chips). Implement create, list, open, play, delete. Levels are separate from the built-in level list and from the chip library.

### Level asset / data model
- Define a level format (e.g. LevelDescription or similar) that holds: I/O pin specs + positions, array of test cases. Each test case: input values, step count, expected output values. Ensure save/load and that the creator’s circuit is never serialized for the level—only the I/O and test data.

### "Make level" entry point
- Add UI path(s) to start creation: e.g. context menu on a chip in the scene ("Make level from this chip") and/or action in chip library. Flow: pick chip → extract I/O and positions from the chip → open recording UI.

### Recording UI
- Screen/mode where creator can: (1) see current I/O (from the chip); (2) add test cases manually (set inputs, optional Step button, Record); (3) or "Run all combinations" to fill test cases (0 steps); (4) list of test cases with reorder/delete; (5) save to level library with name (and optional description).

### Playing and validation
- When loading a user-created level, spawn only the I/O nodes at the stored positions. Reuse existing level play/validation where possible; extend so validation runs the player’s circuit against **all** stored test cases (input + steps → compare output). Pass only when every test case matches.

### Edge cases
- **Clock:** Recording UI must allow "Step" so sequential behaviour can be captured. One test case = one (input, steps, output).
- **Limits:** Consider max test cases per level and/or max input count for "run all combinations" to avoid UI freeze or huge files.

---

## Success criteria

- Creator can start "Make level" from a chip and name the level.
- Creator can record many test cases (manual and/or run all combinations); recording supports optional clock steps per test case.
- Level is saved to the new level library; only I/O + positions + test cases are stored; creator’s circuit is never exposed.
- Player can open a user-created level, see only I/O nodes, build a circuit, and submit; validation passes when all test cases match.
- No regressions to existing built-in levels or level play.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Reports/Ticket_099_User_Created_Levels_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.
