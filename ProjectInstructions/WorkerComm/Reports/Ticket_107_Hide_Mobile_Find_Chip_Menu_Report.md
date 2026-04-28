# Ticket 107 – Report: Hide mobile “Find chip” menu entry

**Status:** Completed  
**Date:** 2026-04-28

## Summary

Removed **FIND CHIP** from the mobile (`UNITY_ANDROID || UNITY_IOS`) `menuButtonNames` array in `BottomBarUI.cs`. Mobile-specific button index constants were moved into the mobile `#if` block (indices 0–9); desktop keeps the original 11 entries and indices including `FindChipButtonIndex`. `OpenSearchMenu()` is only invoked from `ButtonPressed` on non-mobile builds.

## Files touched

- `Assets/Scripts/Graphics/UI/Menus/BottomBarUI.cs`
- `Assets/Resources/patchNotes.json` — user-facing line under **2.1.6.14** → `improvements` (rolled from skipped 2.1.6.13 store submission)

## Testing notes

- Switch build target to Android / iOS: confirm MENU list has no FIND CHIP; LIBRARY, SAVE CHIP, etc. still open correct menus.
- Editor Windows / standalone: FIND CHIP still appears and opens search.

## Follow-up (optional)

- External keyboard on tablet may still trigger `SearchShortcutTriggered` in `ChipInteractionController` (unchanged). Remove or gate separately if product wants zero search entry on mobile.
