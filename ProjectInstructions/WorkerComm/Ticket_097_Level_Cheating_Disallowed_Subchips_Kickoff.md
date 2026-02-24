# Ticket 097: Level cheating – reject solutions with disallowed subchips

You are working on the Digital Logic Sim Mobile Unity project. This is a **bug fix**: re-implement the level zero-score loophole fix. Solutions that contain **disallowed components inside custom chips** (e.g. ROM nested in a custom chip) must be **rejected at validation/upload** so they cannot get a score or upload to the leaderboard.

**Read first:** `ProjectInstructions/Level_Zero_Score_Loophole_Investigation.md` – it explains the loophole, why the previous “fix” never made it into code, and what to implement.

---

## Context

- **Loophole:** In level mode, top-level placement of ROM, Button, Clock, etc. is already restricted (see `ChipInteractionController.IsSpecialChipDisabledInLevel` and `ShouldHideChipInLevel`). But a **custom chip** created outside level mode can contain those components inside it. When the player uses that custom chip in a level, the game does **not** check subchips – so the solution validates and can upload a score (e.g. 0 NANDs = score 0).
- **Current behaviour:** `LevelValidator` only runs test vectors (correctness). Score = `GetNandGateCount()` → `MobileSimulationAdapter.CountNandGates()`. There is **no** check for disallowed chip types anywhere in the hierarchy. Upload in `LevelValidationPopup` is not blocked.
- **Goal:** Before a level solution can be considered valid for score/upload, recursively check the solution’s chip hierarchy. If any chip (including inside custom chips) is a type that is “disallowed in levels”, treat the solution as **invalid** and block upload with a clear message.

---

## Definition of “disallowed in levels”

Use the **same** set of chip types that are already disabled for placement in level mode:

- **In/Out pins** (level-provided only): `ShouldHideChipInLevel` in `ChipInteractionController.cs` – `In_Pin`, `Out_Pin` (these are allowed as level-provided pins; the check you add is for **subchips** in the solution, so custom chips may not add extra In/Out beyond what the level provides – but the main concern is “special” chips).
- **Special chips disabled in level:** `IsSpecialChipDisabledInLevel` in `ChipInteractionController.cs` (lines 35–59): ROM (Rom_256x16), EEPROM, RAM, SevenSegmentDisplay, DisplayRGB, DisplayRGBTouch, DisplayDot, DisplayLED, DisplayRGBLED, Pulse, Clock, Key, Button, Toggle, Detector, Buzzer, RTC, SPS, Constant_8Bit. Consider including **all ROM variants** (e.g. Rom_2x8, Rom_4x4, etc.) if not already covered – `ChipTypeHelper.IsRomType()` lists them.

Recommendation: add a **single central** helper, e.g. `ChipTypeHelper.IsDisabledInLevels(ChipType type)`, that returns true for exactly the types that must not appear anywhere in a level solution. Implement it by reusing the same logic as `IsSpecialChipDisabledInLevel` (and optionally `ShouldHideChipInLevel` for consistency), so UI and validation stay in sync. Then use this helper for the new validation.

---

## What to do

1. **Central “disallowed in levels” (optional but recommended)**  
   In `ChipTypeHelper.cs`, add `IsDisabledInLevels(ChipType type)` that returns true for all chip types that are not allowed in level solutions (same set as `ChipInteractionController.IsSpecialChipDisabledInLevel`, plus any ROM variants you want to exclude). Ensure it matches what the UI uses so we don’t have two different lists.

2. **Recursive “contains disallowed subchip” check**  
   Implement a function that, given a chip (e.g. `DevChipInstance` or the root of the solution), walks the full hierarchy (main chip’s subchips, their subchips, etc.) and returns true if **any** chip in the tree has a type that is disallowed in levels. For custom chips, you need to recurse into their subchips; for built-in chips, use their `ChipType`. Use the central helper from step 1 (or the same logic as `IsSpecialChipDisabledInLevel` if you don’t add ChipTypeHelper).

3. **Integrate into validation/upload flow**  
   - In **LevelValidationPopup**: before “Calculate score” / before allowing upload, run the “contains disallowed subchips” check on the **current viewed chip** (the level solution). If it returns true:
     - Do **not** allow upload (disable or block the upload path).
     - Show a clear message, e.g. “This solution uses components that are not allowed in levels (e.g. ROM inside a custom chip). Remove them to submit a valid score.”
   - Ensure that when the check fails, the score is not uploaded and the user cannot bypass the block.

4. **UX**  
   - When the solution contains disallowed subchips: show the message and do not upload.  
   - When the solution does not contain disallowed subchips: behaviour unchanged (validate, show score, allow upload if passed).

---

## Files to look at

- `ProjectInstructions/Level_Zero_Score_Loophole_Investigation.md` – full context and options.
- `Assets/Scripts/Game/Interaction/ChipInteractionController.cs` – `IsSpecialChipDisabledInLevel`, `ShouldHideChipInLevel` (lines 27–59); placement/delete/duplicate use these.
- `Assets/Scripts/Description/Helpers/ChipTypeHelper.cs` – add `IsDisabledInLevels` here if you centralize.
- `Assets/Scripts/Graphics/UI/Menus/LevelValidationPopup.cs` – where score is calculated (`GetNandGateCount()`), upload is triggered, and where to add the block + message.
- `Assets/Scripts/LevelsIntegration/MobileSimulationAdapter.cs` – `CountNandGates()` (score); you only need to add a *separate* check for disallowed subchips, not change NAND count.
- `Assets/Scripts/Levels/LevelValidator.cs` – currently only test vectors; you can add a constraint check here (Option B in the investigation doc) or keep everything in LevelValidationPopup (Option A).

---

## Success criteria

- Any level solution that contains a disallowed chip type **anywhere** in its hierarchy (including inside custom chips) cannot upload a score and the user sees a clear message explaining why.
- The set of “disallowed” chip types matches what is already disabled for placement in level mode (no new loopholes).
- No regressions: valid solutions (no disallowed subchips) still validate, show score, and upload as before.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Ticket_097_Level_Cheating_Disallowed_Subchips_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.
