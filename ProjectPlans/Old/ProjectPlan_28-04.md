## Project Plan

---

### 💡 Ideas

- Add rotating camera functionality (camera currently fixed).

---

### ✨ Open Tickets

| ID         | Name                                               |   |
| ---------- | -------------------------------------------------- | - |
| TICKET-014 | Clean Up References to Keyboard Shortcuts          |   |
| TICKET-015 | Clean Up Preferences and Settings Menus for Mobile |   |
| TICKET-016 | Scan and Merge Updates from Seb's Repository       |   |
| TICKET-017 | Adapt "Customization View" for Mobile              |   |
| TICKET-018 | Add Mobile UI Customization Mode                   |   |

---

### ✅ Completed Tickets

| ID         | Name                                          |
| ---------- | --------------------------------------------- |
| TICKET-001 | Android Test Build                            |
| TICKET-002 | Codebase Exploration                          |
| TICKET-003 | Create TouchInputHelper                       |
| TICKET-004 | Add Confirm Buttons for Placement             |
| TICKET-005 | Enable Android Keyboard Input                 |
| TICKET-006 | Improve UI Scaling and Black Bars             |
| TICKET-007 | Fix Wire Placement with Confirm Button        |
| TICKET-008 | Fix Camera Movement and Zoom (Multitouch)     |
| TICKET-009 | Add Wrench Tool for Interacting with Elements |
| TICKET-010 | Add Box-Select Tool                           |
| TICKET-011 | Add TrashCan Tool for Deleting Selection      |
| TICKET-012 | Add Copy Tool for Duplicating Selection       |
| TICKET-013 | Hide Mobile UI Buttons Outside Play Mode      |

---

### 📜 Ticket Descriptions

---

#### [TICKET-011] Add TrashCan Tool for Deleting Selection

- Add a "trash can" button.
- Only visible when there are elements selected.
- Tapping the button deletes all selected elements.
- Should feel natural and responsive.

**Result:**

- Trash can tool implemented.
- Deletes selected elements properly when pressed.
- Tool only shows when selection exists.

---

#### [TICKET-012] Add Copy Tool for Duplicating Selection

- Add a "copy" button.
- Duplicates all currently selected elements and prepares them for placement.
- Mimics Shift+D behavior on PC.
- Icon ideas: overlapping squares, copy-paper icon.

**Result:**

- Copy tool implemented.
- Selected elements can now be duplicated and placed easily.
- Matches PC Shift+D behavior on mobile.

---

#### [TICKET-013] Hide Mobile UI Buttons Outside Play Mode

- Hide Mobile UI Tools (Wrench, Box-Select, TrashCan, Copy) when not in Play Mode.
- Search for how Play Mode is detected in existing logic.
- Keep UI clean when in menus or editor views.

**Result:**

- Mobile UI Tools now correctly hidden outside of Play Mode.
- Play Mode detection integrated cleanly.

---

#### [TICKET-014] Clean Up References to Keyboard Shortcuts

- Identify any unnecessary checks for PC keyboard shortcuts.
- Remove or conditionally disable them for mobile.
- Keep code clean and mobile-focused.

---

#### [TICKET-015] Clean Up Preferences and Settings Menus for Mobile

- Remove any irrelevant settings like "show FPS counter" or "keyboard shortcut remapping."
- Hide or adjust menus to suit mobile expectations.
- Keep preferences simple and clean for mobile users.

---

#### [TICKET-016] Scan and Merge Updates from Seb's Repository

- Review Seb's latest changes on the original main branch.
- Identify useful improvements, bug fixes, or enhancements.
- Carefully integrate compatible changes.
- Preserve mobile-specific adjustments made so far.

---

#### [TICKET-017] Adapt "Customization View" for Mobile

- Update the "Customization View" UI used when saving new chips.
- Ensure layout, touch controls, and appearance are mobile-friendly.
- Hide PC-specific features if needed.
- Maintain clean and intuitive experience for mobile users.

---

#### [TICKET-018] Add Mobile UI Customization Mode

- Implement a "customization" mode for Mobile UI.
- Allow users to drag and reposition mobile UI buttons to where they feel most natural.
- Save user-selected layouts.
- Improve overall flexibility and personal comfort when using the app.

