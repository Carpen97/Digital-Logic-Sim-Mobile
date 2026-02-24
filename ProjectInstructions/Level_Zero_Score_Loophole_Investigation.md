# Level zero-score loophole – why the fix is not live

## What Ticket 089 was supposed to fix

**Loophole:** Players could get a score of 0 on levels by placing a **custom chip that contained disallowed components** (e.g. ROM) inside it. Level mode restricts which chips you can place *top-level* (ROM, buttons, etc. are hidden), but a custom chip created outside level mode can contain a ROM; when you use that custom chip in a level, the ROM is nested inside it. The game was not checking subchips, so the solution could validate and upload a score (e.g. 0 NANDs = score 0).

**Intended fix:** Level validation (or score upload) should **check subchips recursively** and **reject** (or treat as invalid) any solution that contains disallowed chip types (ROM, Clock, Button, etc.) anywhere in the hierarchy.

## What the codebase actually does today

- **Score** = `GetNandGateCount()` → `MobileSimulationAdapter.CountNandGates()` (recursively counts NAND gates only). There is **no check** for disallowed chip types in subchips.
- **LevelValidator** only runs test vectors (correctness). It does **not** check for disallowed components.
- **Upload flow** (LevelValidationPopup): validate → calculate score → create solution → upload. **Nothing** prevents uploading when the solution contains a custom chip that has ROM (or other disallowed types) inside it.
- **ChipTypeHelper** has no `IsDisabledInLevels()` in the current code. CompletedTickets for Ticket 047 mention that method; it may have existed in another branch or been removed.

So: **the loophole fix is documented as done (Ticket 089) but the implementation is not present in the current code.**

### How it happened (git investigation)

- **Ticket 089 was added to CompletedTickets.md in commit `6e5479c`** (“Ticket 075 & 078: Wire edit segment deletion, delete-mode ghost hitbox”, 2026-02-10).
- That commit only contains code for 075 and 078: `ChipInteractionController.cs`, `MobileUIController.cs`, patch notes, and doc updates. **No level validation, no subchip checks, no ChipTypeHelper changes.**
- **There is no commit in the repo (any branch) that ever added** `IsDisabledInLevels`, recursive subchip checks, or disallowed-component validation. So the 089 fix was **never committed**.
- Conclusion: **The documentation was updated to say 089 was done, but the actual implementation was never in a commit.** Most likely the fix was either (a) done in a session that never got committed and the doc was updated from memory/assumption, or (b) the doc was updated in the same batch as 075/078 and 089 was mistakenly marked complete without a separate code commit.

## What needs to be done (new ticket)

1. **Define “disallowed in levels”**  
   Central list of chip types that are not allowed in level solutions (ROM, EEPROM, Clock, Pulse, Button, Key, Toggle, displays, etc.). Optionally add `ChipTypeHelper.IsDisabledInLevels(ChipType)` and use the same list as the UI that hides chips in level mode (BottomBarUI, ChipInteractionController, etc.).

2. **Recursive check**  
   Before accepting a level solution (validation pass and/or before score upload), walk the solution’s chip hierarchy (main chip → subchips → their subchips, etc.). If any chip is a disallowed type, treat the solution as **invalid for this level**.

3. **UX**  
   When the solution is invalid because of disallowed subchips:
   - Do **not** upload the score (or show score as invalid).
   - Show a clear message, e.g. “This solution uses components that are not allowed in levels (e.g. ROM inside a custom chip). Remove them to submit a valid score.”

4. **Where to implement**  
   - Option A: In **LevelValidationPopup** before “Calculate score” / upload: run a “contains disallowed subchips” check on the current viewed chip (level solution). If true, block upload and show the message.
   - Option B: In **LevelValidator** or a shared level-rules module: add a constraint check that fails validation when disallowed subchips are present, and have the popup treat failed validation as “invalid for score”.

Result: the loophole is closed again; solutions with nested ROM (or other disallowed chips) cannot get a valid score or upload.
