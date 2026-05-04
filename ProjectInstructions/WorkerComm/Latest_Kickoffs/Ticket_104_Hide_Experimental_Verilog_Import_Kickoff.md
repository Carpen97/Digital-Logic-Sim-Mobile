# Ticket 104: Hide experimental Verilog import from end users

You are working on the Digital Logic Sim Mobile Unity project. **Experimental Verilog import** is implemented (`VerilogParser.cs`, `VerilogImporter.cs`, `VerilogImportMenu.cs`) and reachable from the **bottom bar** (`BottomBarUI.cs`: `VerilogImportButtonIndex`, opens `UIDrawer.MenuType.VerilogImport`). This feature is **not ready to ship**. The goal is to **hide all UI entry points** in production so end users never see or use it, **without deleting** the code so development can continue later.

---

## What to do

1. **Identify every user-facing entry point** to Verilog import (bottom bar button, any other menu or shortcut). List them in your report.

2. **Gate behind a single clear rule**, for example (pick one or combine as appropriate):
   - **Scripting define** (e.g. `VERILOG_IMPORT_EXPERIMENTAL`) — only when defined in Editor or in a dedicated dev Player Settings profile is the UI shown; **not** defined for release Android/PC builds.
   - **`UNITY_EDITOR` only** — import UI only in Editor play mode (not in built player at all).
   - **`Debug.isDebugBuild`** — only in Development Build (document that release builds must not be Development Build for store).

   Prefer a solution that **cannot be accidentally enabled** on a store AAB/APK (e.g. scripting define off by default in release, or Editor-only).

3. **Bottom bar:** When gated off, the Verilog import slot should not appear as a blank hole—adjust layout/counts so the bar looks correct (same pattern as if a button were removed for that build).

4. **Safety:** Ensure `VerilogImport` menu cannot be opened if the button is hidden (no dead code path from other callers).

5. **Documentation:** Short comment at the gate (which define / flag controls visibility). Optionally one line in `ProjectInstructions/Tickets/Ticket_104_Hide_Experimental_Verilog_Import.md` or README for how to turn it on for experiments.

---

## Out of scope

- Removing or rewriting `VerilogImporter` / `VerilogParser`.
- Polishing Verilog import UX for end users (future ticket).

---

## Success criteria

- Release/store build: no Verilog import in UI.
- Documented way for developers to enable it when experimenting.
- Report filed at `ProjectInstructions/WorkerComm/Reports/Ticket_104_Hide_Experimental_Verilog_Import_Report.md`.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Reports/Ticket_104_Hide_Experimental_Verilog_Import_Report.md`** with Status, Summary, and what you did (see `WorkerComm/README.md` for the report template).
