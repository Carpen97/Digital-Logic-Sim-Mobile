# Ticket 105: Bottom bar chip strip – touch drag to scroll (Android / mobile)

You are working on the Digital Logic Sim Mobile Unity project. The **bottom bar** shows the starred chip list (`BottomBarUI.cs`). When there are many items, the strip scrolls horizontally. **PC:** scroll wheel and **middle-mouse drag** move `scrollX`. **Android/iOS:** **← / →** buttons move `scrollX` when shown. Mobile users lack a **finger-drag** way to pan the strip (like middle-mouse on PC). Implement **touch-drag horizontal scrolling** on the masked chip-button region so Android (and iOS) can scroll naturally.

---

## Current implementation (read first)

Open **`Assets/Scripts/Graphics/UI/Menus/BottomBarUI.cs`**:

- After the MENU button, `#if UNITY_ANDROID || UNITY_IOS` draws scroll arrows (unless `showScrollingButtons == 2`).
- Inside `CreateMaskScopeMinMax` (chip strip):
  - `MouseIsOverBar()` + `InputHelper.MouseScrollDelta` adjusts `scrollX`.
  - `InputHelper.IsMouseDownThisFrame(MouseButton.Middle)` starts `isDraggingChipBar`; move updates `scrollX`; `MouseButton.Middle` up ends drag.

**Problem:** On mobile there is no middle mouse; users depend on small arrow buttons.

---

## What to implement

1. **Detect touch drag** over the same region where chip buttons are drawn (masked chip bar). Reuse or mirror the logic used for `isDraggingChipBar` / `mouseDragPrev`, but driven by **primary touch** (or unified pointer API if the project already abstracts touch as mouse—check `InputHelper` and mobile UI patterns elsewhere, e.g. `TwoFingerRigidTransform` or scroll views).

2. **Drag threshold:** Require movement beyond a few pixels before treating as a scroll drag, so **taps** on chip buttons still register as button presses (do not steal all touches from buttons).

3. **Clamp `scrollX`** the same as today after drag (`Mathf.Min(0, chipButtonRegionWidth - chipBarTotalWidthLastFrame)` to `0`).

4. **Optional:** If touch begins on empty padding inside the mask but not on a button, dragging should still scroll (nice-to-have).

5. **Test:** Long starred list on Android device or emulator: drag scrolls; tap still places/opens collection; arrows still work.

---

## Related (not duplicate)

- **Ticket 056** (completed): scroll *views* content dragging—different UI.
- **Ticket 050** (`Ticket_050_Navigation_Specification.md`): nested collections / navigation spec—reference only.

---

## Success criteria

- Horizontal finger drag on the bottom chip strip scrolls it on Android (and iOS if the same code path applies).
- Chip tap / long-press / context menu behaviour remains correct with a sensible drag threshold.
- No regression on PC (wheel + middle drag unchanged).

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Reports/Ticket_105_Bottom_Bar_Touch_Drag_Scroll_Report.md`** with Status, Summary, and what you did (see `WorkerComm/README.md` for the report template).
