# Ticket 074: Simulation speed not saving – Report

**Status:** Done

**Summary:** Fixed the bug where simulation settings (Steps per clock tick, Steps per second target) were reset to 250/1000 when the user confirmed the preferences menu with the Simulation section collapsed. The fix applies the same pattern as Display and Editing: only read and assign simulation form values when the Simulation section is expanded.

**What I did:**

- **PreferencesMenu.cs** – Reworked the "Handle changes" block for the Simulation section:
  - Initialize `simTargetStepsPerSecond`, `simStepsPerClockTick`, and `pauseSim` from `project.description` (existing values).
  - When `IsSimulationSectionExpanded()` is true: read from `ID_ClockSpeedInput` and `ID_SimFrequencyField`, parse, validate (defaults for invalid values), assign to the three variables, and update `lastSimTickRateSetTime`.
  - When collapsed: do not read from the UI; leave the three variables unchanged (they keep the existing `project.description` values).
  - Always assign the final variables to `project.description` at the end, so collapsed case writes the same values back (no overwrite).

**What's left:** Nothing. Ready for PM to update the plan and close the ticket.
