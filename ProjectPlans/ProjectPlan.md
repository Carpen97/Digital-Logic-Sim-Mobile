## Project Plan

---

### 🌟 Open Tickets

| ID         | Name                                      |   |
| ---------- | ----------------------------------------- | - |
| TICKET-006 | Improve UI Scaling and Black Bars         |   |
| TICKET-008 | Fix Camera Movement and Zoom (Multitouch) |   |
| TICKET-010 | Add Box-Select Tool                       |   |

---

### ✅ Completed Tickets

| ID         | Name                                          |
| ---------- | --------------------------------------------- |
| TICKET-001 | Android Test Build                            |
| TICKET-002 | Codebase Exploration                          |
| TICKET-003 | Create TouchInputHelper                       |
| TICKET-004 | Add Confirm Buttons for Placement             |
| TICKET-005 | Enable Android Keyboard Input                 |
| TICKET-007 | Fix Wire Placement with Confirm Button        |
| TICKET-009 | Add Wrench Tool for Interacting with Elements |

---

### 📜 Ticket Descriptions

---

#### [TICKET-001] Android Test Build

- Build and run the current project on an Android device.
- Check:
  - Does the game boot and run?
  - Are resolution and UI scaling acceptable?
  - Does any touch input work (even partially)?
- Take notes on major problems observed.
- **Outcome:** Purely observation. No code changes yet.

**Result:**

- Game boots and runs.
- UI buttons work natively via touch.
- Input based on mouse hover (e.g., placing chips following cursor) does not work on mobile.
- Text input in input fields requires a physical keyboard and does not work on mobile.
- Resolution is acceptable but has minor black bars on the sides.

---

#### [TICKET-002] Codebase Exploration

- Download the provided Assets zip (or receive it via project file).
- Browse core systems:
  - Input handling
  - UI menus and keyboard usage
  - Wire editing and chip interaction
- Plan next tickets based on exploration.
- **Outcome:** List of suggested tickets for input handling, UI scaling fixes, touch improvements, etc.

---

#### [TICKET-003] Create TouchInputHelper

- Create a new class to abstract basic mobile touch input.
- Detect simple tap and hold gestures.
- Mirror InputHelper methods for touch devices.
- Ensure future mobile input changes can reuse this helper easily.

**Result:**

- Implemented `TouchInputHelper.cs` with basic tap, hold, and release detection.
- World-space touch position method added.
- File placed alongside InputHelper.cs for organization.
- Ready for integration into interaction controllers in future tickets.

---

#### [TICKET-004] Add Confirm Buttons for Placement

- When placing a chip or wire, display two buttons: green (confirm) and red (cancel).
- Tap-to-move behavior will act as hovering does on PC.
- Chip or wire will "ghost move" to tapped location without instantly placing.
- Player must tap the green button to confirm placement.
- Cancel button reverts the placement.

**Result:**

- Confirm and Cancel buttons added.
- Placement now requires confirmation, avoiding accidental placement on mobile.
- Ghost movement behavior implemented.

---

#### [TICKET-005] Enable Android Keyboard Input

- Adapt MenuInputField or equivalent to open Android TouchScreenKeyboard.
- Trigger TouchScreenKeyboard.Open() when selecting a text field on mobile.
- Ensure smooth user experience without needing external hardware keyboard.

**Result:**

- Android TouchScreenKeyboard now opens properly.
- No hardware keyboard needed.
- Smooth input field experience achieved.

---

#### [TICKET-006] Improve UI Scaling and Black Bars

- Investigate black bar appearance on mobile screens.
- Adjust resolution, canvas scaler settings, or aspect ratio settings.
- Ensure menus and UI elements fit nicely across different devices.

---

#### [TICKET-007] Fix Wire Placement with Confirm Button

- On mobile, tapping the green confirm button should add a new waypoint for wire placement.
- Tapping on another pin should finalize the wire.
- Wire should behave naturally without double-tapping or missing inputs.

**Result:**

- Confirm button now adds waypoints while drawing wires.
- Tapping another pin finalizes wire placement.
- Placement feels intuitive and reliable.

---

#### [TICKET-008] Fix Camera Movement and Zoom (Multitouch)

- Implement camera panning using two-finger drag.
- Implement zooming in and out using pinch gestures.
- Ensure camera movement and zoom scaling feels smooth and natural.
- Avoid sudden jumps or sensitivity issues.

---

#### [TICKET-009] Add Wrench Tool for Interacting with Elements

- Add a "wrench" button.
- When active, the wrench tool allows tapping on chips, pins, or wires to trigger their standard left-click functionality.
- Same behavior as mouse left-click on PC.
- Important for mobile devices where hover/click separation is missing.

**Result:**

- Wrench tool button implemented.
- Tapping chips, pins, and wires now correctly triggers interaction.
- Smooth functionality matching PC behavior.

---

#### [TICKET-010] Add Box-Select Tool

- Add a "box-select" button.
- When active, allows player to drag-select a rectangle.
- Upon confirming the selection, elements inside the box will be selected.
- Mimic existing PC behavior of click-dragging to select multiple chips, pins, etc.
- Use `IsCreatingSelectionBox` and `SelectionBoxCentre` / `SelectionBoxSize` logic.

#### [TICKET-006] Improve UI Scaling and Black Bars

Investigate black bar appearance on mobile screens.

Adjust resolution, canvas scaler settings, or aspect ratio settings.

Ensure menus and UI elements fit nicely across different devices.

Result:

Black bars are still present, but mobile UI is now properly scaled and positioned.

No UI overlap with black bars. Experience improved.

#### [TICKET-007] Fix Wire Placement with Confirm Button

On mobile, tapping the green confirm button should add a new waypoint for wire placement.

Tapping on another pin should finalize the wire.

Wire should behave naturally without double-tapping or missing inputs.

Result:

Confirm button now adds waypoints while drawing wires.

Tapping another pin finalizes wire placement.

Placement feels intuitive and reliable.

#### [TICKET-008] Fix Camera Movement and Zoom (Multitouch)

Implement camera panning using two-finger drag.

Implement zooming in and out using pinch gestures.

Ensure camera movement and zoom scaling feels smooth and natural.

Avoid sudden jumps or sensitivity issues.

Result:

Camera movement and zoom now work naturally with multitouch.

Scaling is consistent with finger positions.

Smooth user experience across devices.

#### [TICKET-009] Add Wrench Tool for Interacting with Elements

Add a "wrench" button.

When active, the wrench tool allows tapping on chips, pins, or wires to trigger their standard left-click functionality.

Same behavior as mouse left-click on PC.

Important for mobile devices where hover/click separation is missing.

Result:

Wrench tool button implemented.

Tapping chips, pins, and wires now correctly triggers interaction.

Smooth functionality matching PC behavior.

#### [TICKET-010] Add Box-Select Tool

Add a "box-select" button.

When active, allows player to drag-select a rectangle.

Upon confirming the selection, elements inside the box will be selected.

Mimic existing PC behavior of click-dragging to select multiple chips, pins, etc.

Use IsCreatingSelectionBox and SelectionBoxCentre / SelectionBoxSize logic.

Result:

Box-select tool implemented.

Dragging now selects multiple elements correctly on mobile.

Matching behavior to PC version.

