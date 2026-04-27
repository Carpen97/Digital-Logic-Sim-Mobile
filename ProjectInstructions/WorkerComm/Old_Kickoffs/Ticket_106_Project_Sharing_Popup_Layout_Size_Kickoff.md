# Ticket 106: Project Sharing popups – larger list windows (screen fractions)

You are working on the Digital Logic Sim Mobile Unity project. **Project Sharing** in **`MainMenu.cs`** shows scrollable lists in several popups. The PM reports **text overflow** because the list windows are too small. Enlarge the **Export project** (upload list), **Import projects**, and **My projects** popups using **viewport fractions**:

- **Horizontal:** content/list region should span **0.2 to 0.8** of full UI width → usable width **= 0.6 × `Seb.Vis.UI.UI.Width`** (centered unless the codebase prefers explicit min/max positions).
- **Vertical:** same **0.2 to 0.8** of full UI height → usable height **= 0.6 × `Seb.Vis.UI.UI.Height`** for the main scroll/list band (or the whole modal content—choose what fits best with title + filters + buttons).

Start from these numbers and **tweak offsets** (title, `Popup_ScrollOffsetFrac`, filter rows, button rows) if anything clips.

---

## Where to edit

**File:** `Assets/Scripts/Graphics/UI/Menus/MainMenu.cs`

**Existing constants** (around line ~961):

```csharp
const float Popup_TitleOffsetFrac = 0.18f;
const float Popup_ScrollOffsetFrac = 0.12f;
const float Popup_ScrollWidthFrac = 0.52f;
const float Popup_ScrollHeightFrac = 0.28f;
```

These drive `scrollSize` and related widths in:

- `DrawProjectSharingUploadConfirmPopup` — “Upload project” list (`ID_ProjectSharing_UploadScrollView`)
- `DrawProjectSharingImportListPopup` — “Import projects” (`ID_ProjectSharing_LibraryScrollView`)
- `DrawProjectSharingMyProjectsPopup` — “My projects” (`ID_ProjectSharing_MyProjectsScrollView`)

Prefer **named constants** for the new layout (e.g. `ProjectSharing_PopupContentMinFrac = 0.2f`, `MaxFrac = 0.8f`, or `ProjectSharing_ScrollWidthFrac = 0.6f`, `ScrollHeightFrac = 0.6f`) so future tuning is one place.

---

## Layout tasks

1. **Scroll view size:** Set scroll width/height to match the **0.2–0.8** rule (0.6 of canvas per axis for the list area, unless you split vertical between title/filters/scroll/buttons—in that case give the **scroll view** as much of the 0.2–0.8 vertical band as possible while keeping title and buttons visible).

2. **Consistent widths:** Anywhere these popups use `w * Popup_ScrollWidthFrac` for filters, input fields, or `HorizontalButtonGroup` width, keep them aligned with the **same** content width so nothing is narrower than the scroll view.

3. **Scroll content drawers:** `DrawProjectSharingImportScrollContent`, `DrawProjectSharingMyProjectsScrollContent`, `DrawProjectSharingUploadScrollContent` — rows already use `width` from the scroll view; after widening, check long labels. Optionally replace fixed `WrapText(..., 45)` with something derived from `width` (see existing pattern around line ~2447 using `width` for wrap).

4. **Test:** Long display names + “by username” on Android and PC; empty/loading states; confirm CANCEL/IMPORT/CLOSE still reachable.

---

## Success criteria

- The three list popups (export/upload list, import list, my projects) use a **larger** layout consistent with **horizontal and vertical 0.2–0.8** (60% span); overflow is improved vs current `0.52` × `0.28` scroll.
- Related controls (filters, buttons) align with the new width.
- No regressions to other Project Sharing popups (login, edit entry, etc.) unless they share the same constants—in that case verify they still look acceptable or split constants (list popups vs small dialogs).

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Reports/Ticket_106_Project_Sharing_Popup_Layout_Size_Report.md`** with Status, Summary, and what you did (see `WorkerComm/README.md` for the report template).
