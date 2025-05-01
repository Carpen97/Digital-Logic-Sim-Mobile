## Project Plan

---

### ✨ Ideas

- Add rotating camera functionality (camera currently fixed).
- Support importing and exporting chips between mobile and PC versions.

---

### ✨ Open Tickets

| ID         | Name                                                                 |
| ---------- | -------------------------------------------------------------------- |
| TICKET-018 | Add Mobile UI Customization Mode                                     |
| TICKET-020 | Support Import/Export of Chips between Mobile and PC                 |
| TICKET-022 | Add Mobile Simulation Controls (Play/Pause/Step)                     |
| TICKET-023 | Adjust Mobile UI Button Placement for Fullscreen Toggle              |

---

### ✅ Completed Tickets

| ID         | Name                                               |
| ---------- | -------------------------------------------------- |
| TICKET-010 | Add Box-Select Tool                                |
| TICKET-011 | Add TrashCan Tool for Deleting Selection           |
| TICKET-012 | Add Copy Tool for Duplicating Selection            |
| TICKET-013 | Hide Mobile UI Buttons Outside Play Mode           |
| TICKET-014 | Clean Up References to Keyboard Shortcuts          |
| TICKET-015 | Clean Up Preferences and Settings Menus for Mobile |
| TICKET-016 | Scan and Merge Updates from Seb's Repository       |
| TICKET-017 | Adapt "Customization View" for Mobile              |
| TICKET-021 | Add About Section to Mobile Version                |
| TICKET-024 | Fix Snapping Logic Bug                             |

---

### 📜 Ticket Descriptions

#### [TICKET-018] Add Mobile UI Customization Mode

- Implement a "customization" mode for Mobile UI.
- Allow users to drag and reposition mobile UI buttons to where they feel most natural.
- Save user-selected layouts.
- Improve overall flexibility and personal comfort when using the app.

---

#### [TICKET-020] Support Import/Export of Chips between Mobile and PC

- Allow saving chip designs to a JSON file.
- Implement a way to load/import these designs on both mobile and PC.
- Simplify sharing and transferring designs between devices.
- Ensure compatibility between mobile and desktop versions.

---

#### [TICKET-022] Add Mobile Simulation Controls (Play/Pause/Step)

- Add play, pause, and step buttons to control simulation on mobile.
- Allow toggling visibility of these buttons via the Preferences menu.
- Replace keyboard-based functionality with accessible UI on touch devices.
- Maintain consistency with PC functionality without relying on keyboard shortcuts.

---

#### [TICKET-023] Adjust Mobile UI Button Placement for Fullscreen Toggle

- Investigate UI placement issues when fullscreen mode toggles black bars.
- Ensure mobile UI buttons always remain fully visible and usable.
- Respect changes in screen layout between different Android devices.

---

#### [TICKET-024] Fix Snapping Logic Bug

- Addressed inconsistent snapping when dragging multiple selected elements.
- Fixed by caching initial snap offsets before position updates.
- Ensures smooth and consistent relative movement and alignment.
- Confirmed working as intended across devices.

