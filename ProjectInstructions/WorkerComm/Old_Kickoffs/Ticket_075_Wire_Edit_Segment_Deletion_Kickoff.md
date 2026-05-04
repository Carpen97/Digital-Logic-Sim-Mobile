# Ticket 075: Wire edit – restore segment deletion

You are working on the Digital Logic Sim Mobile Unity project. This is a **bug fix**: wire edit mode still allows adding and moving vertices, but **deleting wire segments no longer works** after changes to delete behaviour. Restore or reimplement wire-segment deletion in edit mode.

## Context

- **Source:** Community report (Lamp).
- **What works:** In wire edit mode, users can add and move vertices (wire points). Edit wire is triggered from the context menu (e.g. "EDIT" on a wire); see `ContextMenu.EditWire` → `ChipInteractionController.EnterWireEditMode`.
- **What’s broken:** Deleting a wire segment (removing a vertex/point) no longer works. The code for deleting a point in edit mode exists (e.g. `DeleteCurrentWireToEditPoint`, `wireToEdit.DeleteWirePoint(wireEditPointIndex)`), but the input or flow that should trigger it is likely being consumed or bypassed by the newer delete/eraser behaviour.

## What to do

1. **Locate wire edit and delete logic**
   - `ChipInteractionController.cs`: wire edit state (`wireToEdit`, `wireEditPointIndex`), `EnterWireEditMode`, `DeleteCurrentWireToEditPoint`, `ExitWireEditMode`, and where delete/click is handled (e.g. around lines 273–295 and touch/click handling for wire edit).
   - `DevSceneDrawer.cs`: `DrawWireEditPoints` (draws edit points and sets `wireEditPointIndex`).
   - Identify where “delete wire point” is (or was) triggered: e.g. key, context menu, or click on an edit point.

2. **Find why segment deletion stopped working**
   - Compare with the new delete behaviour (e.g. eraser mode, delete key, or other delete flows). Check if the same input (key, click, or gesture) is now handled by delete/eraser before wire-edit delete runs, or if the wire-edit delete path is no longer reached.

3. **Restore or reimplement segment deletion**
   - Ensure that in wire edit mode, when a user selects a vertex (edit point) and triggers delete, the corresponding wire point is removed (e.g. call `DeleteWirePoint` / `DeleteCurrentWireToEditPoint` and update state).
   - Preserve existing behaviour: add/move vertices and other wire edit actions must keep working. Do not break eraser or other delete behaviour when not in wire edit mode.

## Success criteria

- In wire edit mode, the user can delete a wire segment (remove a vertex) by the intended input (e.g. delete key, backspace, or context/button as designed).
- Adding and moving vertices in wire edit mode still work.
- No regressions: general delete/eraser behaviour and other interactions remain correct.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Ticket_075_Wire_Edit_Segment_Deletion_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.
