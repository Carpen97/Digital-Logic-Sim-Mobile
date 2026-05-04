# Ticket 104: Hide experimental Verilog import from end users

**Type:** Improvement (release hygiene)  
**Status:** Open

---

## Summary

Experimental **Verilog import** exists: parsing structural Verilog and creating chips/groups (`VerilogParser.cs`, `VerilogImporter.cs`, `VerilogImportMenu.cs`). The feature is exposed from the **bottom bar** (see `BottomBarUI.cs` — `VerilogImportButtonIndex`, `OpenVerilogImportMenu` → `UIDrawer.MenuType.VerilogImport`). This must **not ship** to end users until it is product-ready. **Do not remove** the implementation; **hide** all user-facing entry points in production/store builds while keeping the code available for development and future work.

---

## Requirements

- **Production / release builds:** No visible way for a normal user to open Verilog import (no bottom-bar button, no stray menu items). Behaviour should match “feature does not exist” in the UI.
- **Development:** A clear, documented way to **re-enable** the feature for local testing (e.g. `#if UNITY_EDITOR` + scripting define `VERILOG_IMPORT_EXPERIMENTAL`, or “Development Build” only, or a hidden debug flag—choose one approach and document it in code comments and optionally in `ProjectInstructions/`).
- **Code:** Keep `VerilogImporter`, `VerilogParser`, `VerilogImportMenu`, and related wiring; only gate **UI** and any **public entry points** that could be triggered accidentally.

---

## Success criteria

- Store / release configuration: Verilog import is not discoverable or usable from the UI.
- Editor or dev-flag path: team can still use Verilog import when needed.
- No deletion of the Verilog pipeline code for this ticket.

---

## References (implementation hints)

- `Assets/Scripts/Graphics/UI/Menus/BottomBarUI.cs` — Verilog import button index and `OpenVerilogImportMenu`.
- `Assets/Scripts/Graphics/UI/Menus/VerilogImportMenu.cs`
- `Assets/Scripts/Graphics/UI/UIDrawer.cs` — `MenuType.VerilogImport`
- `Assets/Scripts/SaveSystem/VerilogImporter.cs`, `VerilogParser.cs`
- `ProjectInstructions/Verilog_Behavioral_Synthesis_Investigation.md` (context)

---

## Kickoff

`ProjectInstructions/WorkerComm/Latest_Kickoffs/Ticket_104_Hide_Experimental_Verilog_Import_Kickoff.md`

Report when done: `ProjectInstructions/WorkerComm/Reports/Ticket_104_Hide_Experimental_Verilog_Import_Report.md`
