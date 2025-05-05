## Project Plan

---

### 💡 Ideas

* Add rotating camera functionality (camera currently fixed).

---

### ✨ Open Tickets

| ID         | Name                                                    |
| ---------- | ------------------------------------------------------- |
| TICKET-018 | Add Mobile UI Customization Mode                        |
| TICKET-022 | Add Mobile Simulation Controls (Play/Pause/Step)        |
| TICKET-023 | Adjust Mobile UI Button Placement for Fullscreen Toggle |
| TICKET-027 | Add Mobile UI Buttons for Undo/Redo                     |
| TICKET-028 | Support Export of Projects as Zip Files                 |

---

### ✅ Completed Tickets

| ID         | Name                                      |
| ---------- | ----------------------------------------- |
| TICKET-026 | Sync with Latest Upstream Project Changes |
| TICKET-025 | Add Landscape Orientation Toggle Option   |
| TICKET-024 | Fix Snapping Logic Bug                    |
| TICKET-021 | Add About Section to Mobile Version       |
| TICKET-020 | Support Import of Projects as Zip Files   |

---

### 📜 Ticket Descriptions

#### \[TICKET-018] Add Mobile UI Customization Mode

* Implement a "customization" mode for Mobile UI.
* Allow users to drag and reposition mobile UI buttons to where they feel most natural.
* Save user-selected layouts.
* Improve overall flexibility and personal comfort when using the app.

---

#### \[TICKET-020] Support Import of Projects as Zip Files

* Allow importing entire project folders as `.zip` archives.
* This replaces earlier plans to import single chips, which proved insufficient due to dependencies on sub-chips.
* Implemented UI for selecting zip files and integrating their content into the current file structure.
* Ensures imported projects are platform-neutral and can originate from desktop or mobile.

---

#### \[TICKET-022] Add Mobile Simulation Controls (Play/Pause/Step)

* Add play, pause, and step buttons to control simulation on mobile.
* Allow toggling visibility of these buttons via the Preferences menu.
* Replace keyboard-based functionality with accessible UI on touch devices.
* Maintain consistency with PC functionality without relying on keyboard shortcuts.

---

#### \[TICKET-023] Adjust Mobile UI Button Placement for Fullscreen Toggle

* Investigate UI placement issues when fullscreen mode toggles black bars.
* Ensure mobile UI buttons always remain fully visible and usable.
* Respect changes in screen layout between different Android devices.

---

#### \[TICKET-024] Fix Snapping Logic Bug

* Addressed inconsistent snapping when dragging multiple selected elements.
* Fixed by caching initial snap offsets before position updates.
* Ensures smooth and consistent relative movement and alignment.
* Confirmed working as intended across devices.

---

#### \[TICKET-025] Add Landscape Orientation Toggle Option

* Add a toggle in the preferences menu to flip between landscape-left and landscape-right.
* Improve accessibility for left-handed and right-handed users.
* Implemented using Unity’s Screen.orientation logic.

---

#### \[TICKET-026] Sync with Latest Upstream Project Changes

* Successfully merged the latest changes from Sebastian Lague’s main branch into the mobile-port branch.
* All mobile-specific features (touch input, UI layout, preferences, chip saving, simulation flow) have been tested and continue to work as expected.
* Merge was verified in a temporary branch (`merge-upstream-pass2`) before being finalized.
* The mobile fork is now up to date with upstream.

---

#### \[TICKET-027] Add Mobile UI Buttons for Undo/Redo

* Make the new `TryUndo` and `TryRedo` functions (added in upstream) accessible via the mobile UI.
* Add two new buttons: one for Undo, one for Redo.
* Buttons should call `TryUndo` and `TryRedo` in the appropriate manager class.
* Ensure placement is mobile-friendly and does not interfere with existing UI.
* Behavior should match the desktop version, which is triggered via keyboard.

---

#### \[TICKET-028] Support Export of Projects as Zip Files

* Add option to export the current open project as a `.zip` file.
* Export should include all chip files and metadata required to fully restore the project on another device.
* Ensure exported zip files are compatible with both mobile and desktop versions.
* UI should clearly indicate where the exported file is saved.

---

### 📌 Project Status

The project is in a stable, playable state on mobile. We've completed the major porting and UI adaptation. Testers are now trying out the game and providing useful feedback. A few quality-of-life features are planned to polish the experience, such as simulation controls, flexible layout options, and syncing with upstream development.
