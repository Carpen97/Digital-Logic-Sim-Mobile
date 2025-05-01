## Project Plan

---

### 💡 Ideas

- Add rotating camera functionality (camera currently fixed).
- Support importing and exporting chips between mobile and PC versions.

---

### ✨ Open Tickets

| ID         | Name                                                    |
| ---------- | ------------------------------------------------------- |
| TICKET-018 | Add Mobile UI Customization Mode                        |
| TICKET-020 | Support Import/Export of Chips between Mobile and PC    |
| TICKET-022 | Add Mobile Simulation Controls (Play/Pause/Step)        |
| TICKET-023 | Adjust Mobile UI Button Placement for Fullscreen Toggle |
| TICKET-026 | Sync with Latest Upstream Project Changes               |

---

### ✅ Completed Tickets

| ID         | Name                                    |
| ---------- | --------------------------------------- |
| TICKET-025 | Add Landscape Orientation Toggle Option |
| TICKET-024 | Fix Snapping Logic Bug                  |
| TICKET-021 | Add About Section to Mobile Version     |
| TICKET-017 | Adapt Customization View for Mobile     |
| TICKET-016 | Sync with Upstream Project Changes      |
| TICKET-015 | Clean Up Preferences Menu for Mobile    |
| TICKET-014 | Remove Keyboard Shortcut References     |
| TICKET-013 | Hide Mobile UI Buttons When Not Playing |
| TICKET-012 | Add Copy Tool                           |
| TICKET-011 | Add TrashCan Tool                       |
| TICKET-010 | Add Box-Select Tool                     |
| TICKET-009 | Add Wrench Tool                         |
| TICKET-008 | Fix Zoom Gesture for Mobile             |
| TICKET-007 | Add Click Functionality to Pins         |
| TICKET-006 | Fix Mobile UI Scaling in Fullscreen     |
| TICKET-005 | Android Keyboard Input                  |
| TICKET-004 | Wire Rendering Appearance               |
| TICKET-003 | Line Connection Between Pins            |
| TICKET-002 | Add Logic Gates                         |
| TICKET-001 | Create Basic Project Infrastructure     |

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

---

#### [TICKET-025] Add Landscape Orientation Toggle Option

- Add a toggle in the preferences menu to flip between landscape-left and landscape-right.
- Improve accessibility for left-handed and right-handed users.
- Implemented using Unity’s Screen.orientation logic.

---

#### [TICKET-026] Sync with Latest Upstream Project Changes

- Check for recent changes pushed to Sebastian's main branch.
- Merge those updates into the mobile fork.
- Confirm nothing breaks mobile-specific functionality.
- Ensure parity between desktop and mobile versions where appropriate.

---

### 📌 Project Status

The project is in a stable, playable state on mobile. We've completed the major porting and UI adaptation. Testers are now trying out the game and providing useful feedback. A few quality-of-life features are planned to polish the experience, such as simulation controls, flexible layout options, and syncing with upstream development.

