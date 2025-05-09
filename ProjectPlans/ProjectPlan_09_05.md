## Project Plan

---

### 💡 Ideas

* Add rotating camera functionality (camera currently fixed).

---

### ✨ Open Tickets

\| ID         | Name                                                    |

| TICKET-037 | Investigate Resolution, Aspect Ratio & Fullscreen Behavior |
| ---------- | ---------------------------------------------------------- |
| TICKET-018 | Add Mobile UI Customization Mode                           |
| TICKET-022 | Add Mobile Simulation Controls (Play/Pause/Step)           |
| TICKET-023 | Adjust Mobile UI Button Placement for Fullscreen Toggle    |
| TICKET-028 | Support Export of Projects as Zip Files                    |
| TICKET-030 | Set Up Unity Cloud Build for iOS Deployment                |
| TICKET-033 | Add Video Capture Mode with Start/Stop and Save Options    |
|            |                                                            |

---

### ✅ Completed Tickets

| ID         | Name                                                     |
| ---------- | -------------------------------------------------------- |
| TICKET-029 | Investigate Scrollbar Issues on Low-End Android Devices  |
| TICKET-031 | Add Scroll Buttons for Hotbar on Mobile                  |
| TICKET-032 | Merge Upstream Changes from Version 2.1.6                |
| TICKET-034 | Fix Display Placement Conflict During Customization View |
| TICKET-035 | Fix Wire Edit Gestures Causing Unintended Camera Panning |
| TICKET-036 | Hide Mobile UI Buttons in View-Chip Mode                 |

---

#### \[TICKET-037] Investigate Resolution, Aspect Ratio & Fullscreen Behavior

* Explore how Unity handles resolution settings, aspect ratios, fullscreen, and borderless modes across different devices.
* Identify any inconsistencies or edge cases in how the app renders on tablets, widescreen phones, and small displays.
* Document the current behavior and test adjustments via Unity Player Settings or screen settings at runtime.
* Outcome: deeper understanding of display configuration and a foundation for future UI layout fixes or device-specific tweaks.

---

### 📜 Ticket Descriptions

#### \[TICKET-018] Add Mobile UI Customization Mode

* Implement a "customization" mode for Mobile UI.
* Allow users to drag and reposition mobile UI buttons to where they feel most natural.
* Save user-selected layouts.
* Improve overall flexibility and personal comfort when using the app.

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

#### \[TICKET-028] Support Export of Projects as Zip Files

* Add option to export the current open project as a `.zip` file.
* Export should include all chip files and metadata required to fully restore the project on another device.
* Ensure exported zip files are compatible with both mobile and desktop versions.
* UI should clearly indicate where the exported file is saved.

---

#### \[TICKET-029] Investigate Scrollbar Issues on Low-End Android Devices

* A tester using a Realme C11 (2021) running Android Go reported that they could not scroll through the list of logic gates.
* Issue was verified on similar low-end hardware and mitigated through adjustments to UI scroll behavior.
* Scrollbar behavior has since been verified as functional on low-end Android setups.
* ✅ Ticket resolved — no further issues reported.

---

#### \[TICKET-030] Set Up Unity Cloud Build for iOS Deployment

* Set up Unity Cloud Build to produce iOS builds for testing.
* Requires Unity Pro subscription.
* Configure iOS build target, provisioning profiles, and signing certificates.
* Ensure builds are generated and downloadable via Unity Cloud.
* Begin testing output on physical iOS devices (e.g., iPad).
* This replaces the need for a local Mac or MacStadium.

---

#### \[TICKET-031] Add Scroll Buttons for Hotbar on Mobile

* Replace the original plan for drag-based scrolling with a button-based solution.
* Add left and right arrow buttons directly in or near the hotbar.
* Pressing a button should scroll the hotbar left or right by a fixed amount.
* Ensure the scroll amount is appropriate for fast access but doesn’t skip items.
* UI should be touch-friendly and not obstruct core hotbar functionality.
* Optional: disable buttons when at the far ends of the scroll region.
* ✅ Completed and verified working on real devices.

---

#### \[TICKET-032] Merge Upstream Changes from Version 2.1.6

* Merge Sebastian Lague’s upstream `main` (v2.1.6) into the mobile fork while preserving mobile-specific features.

✅ Merge Process

* Fetched latest upstream `main` (no v2.1.6 tag available).
* Created `merge-v216-mobile-test` off `mobile-port`.
* Resolved all merge conflicts manually.
* Reintegrated mobile-specific objects (TouchInputHelper, Canvas, etc.) by loading scenes additively.

🔊 Audio System Fixes for Buzzer

* Added required `AudioUnity` GameObject (with `AudioUnity` script + AudioSource).
* Fixed sound playback by adding `AudioListener` to Main Camera.
* Verified `OnAudioFilterRead()` runs correctly and buzzer emits valid audio.

✅ Validation Summary (Mobile + Desktop)

* Buzzer chip functions correctly with valid input
* Simulation runs with sound
* Chip library and selection menus work
* Mobile UI (Canvas, TouchInputHelper) preserved
* Undo/Redo, snapping, and color-state fixes verified

🏁 Result

* All upstream v2.1.6 changes merged and verified functional on mobile.
* Mobile-specific features remain intact and working.
* ✅ Ticket completed and ready to merge back into `mobile-port`.

---

#### \[TICKET-033] Add Video Capture Mode with Start/Stop and Save Options

* Create a dedicated mode or overlay UI for capturing gameplay video.
* Provide buttons for **Start Recording** and **Stop Recording**.
* After stopping, prompt user to choose filename and storage location.
* Use platform-specific video capture methods (e.g., Unity Recorder or native APIs).
* Ensure smooth performance during recording on mid- to high-end Android devices.
* Consider file format compatibility and file size limits.

---

#### \[TICKET-034] Fix Display Placement Conflict During Customization View

* Users reported difficulty placing the display element in customization view.
* The issue was due to camera drag input being active while trying to move a display.
* Resolved by temporarily suppressing camera input while dragging display objects.
* Now, placing displays is smoother and no longer interferes with camera panning.
* ✅ Completed and functioning as expected — awaiting further tester feedback.

---

#### \[TICKET-035] Fix Wire Edit Gestures Causing Unintended Camera Panning

* Editing wires with the wrench tool previously caused the camera to pan when dragging wire control points.
* This behavior made it difficult to precisely edit wire paths on mobile.
* The issue has been resolved by preventing camera input while wire points are being manipulated.
* ✅ Verified working on mobile devices — wire editing now functions without screen movement.

---

#### \[TICKET-036] Hide Mobile UI Buttons in View-Chip Mode

* Some mobile UI buttons are still visible in view-chip mode when they shouldn’t be.
* Only the wrench tool should be shown while in this mode.
* Investigate visibility logic and add appropriate conditional checks.
* Ensure that buttons return to expected state when exiting view mode.
* Test across various screen sizes and interaction states.

---

### 📌 Project Status

The project is in a stable, playable state on mobile. We've completed the major porting and UI adaptation. Testers are now trying out the game and providing useful feedback. A few quality-of-life features are planned to polish the experience, such as simulation controls, flexible layout options, and syncing with upstream development.

---

### 🔧 Worker Chat Metadata

**Project Context: Digital Logic Sim – Mobile Version**
You're working on a Unity-based port of the open-source PC game [Digital Logic Sim by Sebastian Lague](https://github.com/SebLague/Digital-Logic-Sim). The mobile version preserves core simulation features while adapting the UI for touchscreen devices.

**Architecture & Style**

* Core logic is shared with upstream
* UI and input handling is mobile-first
* We aim for clean and modular Unity/C# code
* Features are added incrementally via Git branches

**Your Role**

* Complete the ticket described below
* Maintain compatibility with mobile interaction patterns
* Focus on performance and responsiveness on real Android devices
* Log edge cases, bugs, or assumptions in your response

**Helpful Notes**

* Unity version: `6000.0.46f1`
* Android build target: `.aab` with Play Store internal testing
* Project is actively maintained and synced with upstream
* Communication is async, but quick turnaround is appreciated
