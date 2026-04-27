# Ticket 106: Project Sharing popups – larger list windows (screen fractions)

**Type:** Improvement  
**Status:** Done

---

## Summary

**Project Sharing** flows in `MainMenu.cs` open popups with **scrollable lists** of text entries for **Export project** (upload project list), **Import projects**, and **My projects**. Text overflows because the scroll regions are too small. Enlarge these windows using **screen-space fractions**: horizontal span **0.2–0.8** and vertical **0.2–0.8** of the UI canvas (i.e. **60%** width and **60%** height for the main content/list region, centered—or equivalently left at 0.2×width from origin and width = 0.6×width if that matches the project’s coordinate convention). Tune title, filter wheels, scroll view, and button row positions so layout stays coherent.

---

## Scope

- **`MainMenu.cs`** – Project Sharing popups that use `Popup_ScrollWidthFrac`, `Popup_ScrollHeightFrac`, `Popup_ScrollOffsetFrac`, and related layout (currently ~`0.52` × `0.28` for scroll size per constants near `DrawProjectSharingUploadConfirmPopup`, `DrawProjectSharingImportListPopup`, `DrawProjectSharingMyProjectsPopup`).
- Align **button row widths**, **filter row widths**, and **scroll view** to the new horizontal extent so labels use the wider area.
- Optionally improve **text wrapping** in scroll content (e.g. `WrapText(..., 45)` in import empty state) to scale with available width where appropriate.

---

## Success criteria

- Export-project list, Import-projects list, and My-projects list popups use a **noticeably larger** scroll/content area consistent with **~0.2–0.8** horizontal and vertical viewport fractions (iterate if minor tweaks needed for title/buttons).
- Long project names / “by user” lines have **less overflow** or wrap better within the wider list.
- No broken layout on mobile and PC; popups still dismiss correctly.

---

## References

- Kickoff: `ProjectInstructions/WorkerComm/Latest_Kickoffs/Ticket_106_Project_Sharing_Popup_Layout_Size_Kickoff.md`
- Report: `ProjectInstructions/WorkerComm/Reports/Ticket_106_Project_Sharing_Popup_Layout_Size_Report.md`
- Code: `Assets/Scripts/Graphics/UI/Menus/MainMenu.cs` — search `DrawProjectSharingUploadConfirmPopup`, `DrawProjectSharingImportListPopup`, `DrawProjectSharingMyProjectsPopup`, constants `Popup_ScrollWidthFrac` / `Popup_ScrollHeightFrac`.
