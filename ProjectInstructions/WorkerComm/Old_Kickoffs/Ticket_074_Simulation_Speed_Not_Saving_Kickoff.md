# Ticket 074: Simulation speed not saving

You are working on the Digital Logic Sim Mobile Unity project. This is a **bug fix**: the simulation settings in Preferences (Steps per clock tick, Steps per second target) sometimes reset to 250/1000 when the user confirms the preferences menu, so custom values do not persist.

**Read first:** `ProjectInstructions/Simulation_Speed_Not_Saving_Investigation.md` – it explains the root cause and the fix.

---

## Root cause (summary)

In **PreferencesMenu.cs**, when the user clicks **Confirm**:

- The code **always** reads the simulation input fields and assigns `Prefs_SimTargetStepsPerSecond` and `Prefs_SimStepsPerClockTick` to `project.description`.
- When the **Simulation** section is **collapsed**, those inputs are not visible and the code uses **empty** `InputFieldState` instances. Parsing empty text yields 0, then the “invalid value” logic sets **250** and **1000**, and those are written and saved. So confirming with Simulation collapsed **overwrites** the user’s saved sim settings with 250/1000.

Other sections (Display, Editing) only apply their form values when their section is expanded; Simulation should do the same.

---

## What to do

1. **In PreferencesMenu.cs**, in the “Handle changes” block where simulation values are read and assigned:
   - When **Simulation section is expanded**: keep current behaviour (read from `ID_ClockSpeedInput` and `ID_SimFrequencyField`, parse, clamp, assign to `project.description.Prefs_SimStepsPerClockTick` and `Prefs_SimTargetStepsPerSecond`). Also keep reading `pauseSim` from the wheel when expanded.
   - When **Simulation section is collapsed**: **do not** assign `Prefs_SimTargetStepsPerSecond` or `Prefs_SimStepsPerClockTick` (and do not overwrite `Prefs_SimPaused` from the wheel). Leave the existing `project.description` values for those three fields unchanged.

2. **Same pattern as other sections:** Display and Editing already use `IsDisplaySectionExpanded()` / `IsEditingSectionExpanded()` to decide whether to read from the UI or keep existing description values. Apply the same pattern for Simulation: only apply form values when `IsSimulationSectionExpanded()` is true.

3. **Validation:** When the section *is* expanded, keep the existing validation (clockSpeed &lt; 10 → 250, targetStepsPerSecond &lt; 100 → 1000). No change to min/max rules.

---

## Success criteria

- User sets custom “Steps per clock tick” and “Steps per second (target)” in Preferences (Simulation section expanded), clicks Confirm → values persist after reopen.
- User opens Preferences, **does not** expand Simulation, changes e.g. Display only, clicks Confirm → **simulation settings are unchanged** (not reset to 250/1000).
- User expands Simulation, changes sim values, clicks Confirm → new values save as today.
- No regressions: pause/resume sim and other preference behaviour unchanged.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Ticket_074_Simulation_Speed_Not_Saving_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.
