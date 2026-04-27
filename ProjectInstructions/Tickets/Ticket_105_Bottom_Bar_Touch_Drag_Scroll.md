# Ticket 105: Bottom bar chip strip – touch drag to scroll (Android / mobile)

**Type:** Improvement  
**Status:** Done

---

## Summary

The **bottom bar** (`BottomBarUI.cs`) shows starred chips and collections. When the strip **overflows**, users can pan horizontally: on **PC**, the **scroll wheel** and **middle-mouse drag** update `scrollX`. On **Android / iOS**, **← / →** buttons scroll the strip when `showScrollingButtons` allows it. There is **no finger-drag** equivalent to PC’s middle-mouse drag, which makes navigation awkward on touch devices. This ticket adds **horizontal touch drag** on the chip strip (inside the masked region) so Android users can scroll the bar naturally without relying only on arrow taps.

---

## Context (code)

- `BottomBarUI.cs`: `scrollX`, `chipButtonRegionWidth`, `chipBarTotalWidthLastFrame`; chip buttons drawn inside `CreateMaskScopeMinMax`.
- PC path: `MouseIsOverBar()` + `InputHelper.MouseScrollDelta` and `MouseButton.Middle` for `isDraggingChipBar` / `mouseDragPrev`.
- Mobile path: `#if UNITY_ANDROID || UNITY_IOS` arrow buttons adjust `scrollX`.

---

## Success criteria

- On Android (and iOS if applicable), dragging horizontally on the **chip/folder strip** scrolls the strip (same bounds as `scrollX` clamp today).
- Does not break chip **tap** to place / open collection (use a drag threshold so short touches still count as taps).
- Arrows and existing behaviour remain available where they exist today.

---

## References

- Kickoff: `ProjectInstructions/WorkerComm/Latest_Kickoffs/Ticket_105_Bottom_Bar_Touch_Drag_Scroll_Kickoff.md`
- Report: `ProjectInstructions/WorkerComm/Reports/Ticket_105_Bottom_Bar_Touch_Drag_Scroll_Report.md`
