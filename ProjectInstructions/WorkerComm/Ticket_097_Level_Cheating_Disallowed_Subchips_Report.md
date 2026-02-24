# Ticket 097: Level cheating – reject solutions with disallowed subchips – Report

## Status

**Done**

## Summary

Re-implemented the level zero-score loophole fix. Solutions that contain disallowed components anywhere in the chip hierarchy (e.g. ROM nested inside a custom chip) are now rejected at validation/upload, and placement of such chips is blocked when in level mode. All checks apply **only when a level is active** – outside levels, normal placement is unchanged.

## What I did

### 1. Central `IsDisabledInLevels` helper (ChipTypeHelper.cs)

- Added `ChipTypeHelper.IsDisabledInLevels(ChipType type)` returning true for all chip types not allowed in level solutions.
- Covers: In_Pin, Out_Pin (custom chips may not add extra pins beyond level-provided ones); all ROM variants via `IsRomType`; and the special chips from `ChipInteractionController.IsSpecialChipDisabledInLevel` (RAM, displays, Pulse, Clock, Key, Button, Toggle, Detector, Buzzer, RTC, SPS, Constant_8Bit).
- Keeps UI and validation in sync with a single source of truth.

### 2. Recursive “contains disallowed subchip” check (MobileSimulationAdapter.cs)

- Added `ContainsDisallowedSubchips()` which recursively walks the `SimChip` hierarchy (root level solution chip and all subchips).
- Uses `ContainsDisallowedSubchipsRecursive(SimChip chip)` – checks each chip’s `ChipType` against `ChipTypeHelper.IsDisabledInLevels`, recurses into `SubChips`. Returns true as soon as any disallowed type is found.
- `SimChip` already mirrors the full hierarchy including custom chips, so no extra ChipLibrary lookups are needed.

### 3. Recursive check for placement (ChipLibrary.cs)

- Added `ChipDescriptionContainsDisallowedSubchipsForLevel(ChipDescription desc)` – walks `ChipDescription` hierarchy (custom chips → subchips) and returns true if any disallowed type is present.
- Used at placement time (before SimChip exists) to block custom chips that contain nested disallowed components.
- Includes visited-set to avoid infinite recursion on circular chip references.

### 4. Integration into LevelValidationPopup

- **On Open:** Runs `ContainsDisallowedSubchips()` and stores result in `_hasDisallowedSubchips`.
- **UI:** When `_hasDisallowedSubchips`, displays message: “This solution uses components that are not allowed in levels (e.g. ROM inside a custom chip). Remove them to submit a valid score.” (orange text, below score).
- **Buttons:** Upload Score and Save as Chip are disabled when `_hasDisallowedSubchips` (via `canUploadOrSave = levelPassed && !_hasDisallowedSubchips`).
- **Defence in depth:** `UploadToLeaderboard` returns immediately with a status message if `_hasDisallowedSubchips`, preventing any bypass.

### 5. Placement-time blocking (only when in level)

- **BottomBarUI.TryStartPlacing:** Checks `ChipDescriptionContainsDisallowedSubchipsForLevel` for custom chips – blocks placement and shows message when in a level.
- **ChipInteractionController.StartPlacing:** Same check for placement from Library/Search – blocks when in a level.
- **ChipLibraryMenu:** Fixed popup overwrite – when placing disallowed chip from Library, `ExitLibrary()` was called immediately and overwrote the “This chip type is disabled” popup. Now only calls `ExitLibrary()` when placement actually succeeds (`StartPlacing` returns non-null).
- **Level gating:** All nested-disallowed checks are wrapped in `LevelManager.Instance != null && lm.IsActive` – they run **only when in a level**. Outside levels, custom chips with nested ROM etc. can be placed normally.
- **Distinct messages:** Direct disallowed chip → “This chip type is disabled for this level”. Nested disallowed (custom chip contains ROM etc.) → “This chip contains components that are not allowed in levels (e.g. ROM inside it). Remove them to use it.”

### 6. Files touched

- `Assets/Scripts/Description/Helpers/ChipTypeHelper.cs` – Added `IsDisabledInLevels`
- `Assets/Scripts/LevelsIntegration/MobileSimulationAdapter.cs` – Added `ContainsDisallowedSubchips` and recursive helper
- `Assets/Scripts/Graphics/UI/Menus/LevelValidationPopup.cs` – Popup state, Open() check, message display, button gating, upload guard
- `Assets/Scripts/Game/Project/ChipLibrary.cs` – Added `ChipDescriptionContainsDisallowedSubchipsForLevel`
- `Assets/Scripts/Graphics/UI/Menus/BottomBarUI.cs` – Placement-time nested check (level-gated), `ShowNestedDisallowedMessage`
- `Assets/Scripts/Game/Interaction/ChipInteractionController.cs` – Placement-time nested check (level-gated), `ShowNestedDisallowedMessage`
- `Assets/Scripts/Graphics/UI/Menus/ChipLibraryMenu.cs` – Only `ExitLibrary()` when placement succeeds; use 3-arg `StartPlacing` to get return value

## Success criteria (verified)

- [x] Any level solution with a disallowed chip type anywhere in its hierarchy (including inside custom chips) cannot upload a score.
- [x] The user sees a clear message explaining why (at both validation and placement).
- [x] Custom chips with nested disallowed components are blocked at placement when in a level (bottom bar + library).
- [x] Direct disallowed chips (ROM, RAM, etc.) show one message; nested disallowed show a different, specific message.
- [x] All placement checks apply **only when in a level** – outside levels, no blocking.
- [x] The set of disallowed chip types matches what is already disabled for placement in level mode.
- [x] Valid solutions (no disallowed subchips) still validate, show score, and allow upload as before.
- [x] Save as Chip is also blocked for disallowed solutions.
- [x] Placing disallowed chip from Library shows popup (no silent block).

## What's left

Nothing. Ticket complete.
