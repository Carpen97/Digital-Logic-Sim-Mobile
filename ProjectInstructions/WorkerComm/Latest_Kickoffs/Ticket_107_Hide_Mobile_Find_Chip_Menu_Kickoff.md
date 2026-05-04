# Ticket 107: Hide “Find chip” from mobile bottom bar menu

You are on the **Digital Logic Sim Mobile** Unity project. Remove the **FIND CHIP** option from the **MENU** popup on **Android and iOS** only. **PC / desktop** must keep FIND CHIP and existing shortcuts.

---

## What to do

1. **File:** `Assets/Scripts/Graphics/UI/Menus/BottomBarUI.cs`
   - Under `#if UNITY_ANDROID || UNITY_IOS`, remove the `"  FIND CHIP  "` entry from `menuButtonNames`.
   - **Button indices** must stay consistent: either duplicate the `const int …ButtonIndex` block per platform (mobile indices shift by −1 after Find Chip) or skip the row in the loop without leaving gaps (preferred: separate index constants for mobile vs `#else`, same pattern as Verilog gating uses indices).

2. **`ButtonPressed`:** Do not call `OpenSearchMenu()` for mobile menu indices (wrap the find-chip branch in `#if !(UNITY_ANDROID || UNITY_IOS)` or rely on mobile indices never mapping to that action).

3. **Optional:** If `ChipInteractionController` (or elsewhere) opens `UIDrawer.MenuType.Search` on **Ctrl+F** / `SearchShortcutTriggered`, decide whether mobile should still allow that with an external keyboard; default is **menu-only** removal unless product asks to block all search entry on mobile.

4. **Report:** `ProjectInstructions/WorkerComm/Reports/Ticket_107_Hide_Mobile_Find_Chip_Menu_Report.md` — Status, Summary, files touched, test notes (Editor Android build target smoke test).

---

## Out of scope

- Fixing Ticket 069 (library search behaviour).
- Removing Search UI globally.

---

## Success criteria

- Release AAB / iOS: MENU has no FIND CHIP; other items unchanged order except that row gone.
- Desktop unchanged.
- Report filed as above.
