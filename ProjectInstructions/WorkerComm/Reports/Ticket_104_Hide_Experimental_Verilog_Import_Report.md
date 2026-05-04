# Ticket 104 - Hide experimental Verilog import report

Status: Done

Summary: The experimental Verilog import UI is now hidden outside the Unity Editor using a single centralized gate. Bottom bar/menu entry points no longer expose Verilog import in non-Editor builds, and forced menu activation is also blocked.

What I did:
- Identified user-facing entry points:
  - `Assets/Scripts/Graphics/UI/Menus/BottomBarUI.cs` menu label/slot (`VERILOG`) and button action.
  - `Assets/Scripts/Graphics/UI/Menus/BottomBarUI.cs` `OpenVerilogImportMenu()` method.
  - `Assets/Scripts/Graphics/UI/UIDrawer.cs` `MenuType.VerilogImport` rendering path.
- Added a single gate: `Assets/Scripts/Graphics/UI/VerilogImportFeatureGate.cs`.
  - `VerilogImportFeatureGate.IsEnabled` is `true` only under `UNITY_EDITOR`, `false` otherwise.
  - Included a comment documenting this is controlled by Ticket 104 and intended to prevent store/release exposure.
- Updated bottom bar popup menu drawing in `BottomBarUI.cs`:
  - Skips drawing the Verilog menu item when gate is off (no blank hole in menu layout).
  - Verilog button enable logic now also requires the feature gate.
  - Verilog button action only triggers when gate is enabled.
  - `OpenVerilogImportMenu()` now hard-checks gate before opening.
- Added safety guard in `UIDrawer.cs`:
  - Verilog menu is drawn only when gate is enabled.
  - `SetActiveMenu(MenuType.VerilogImport)` is redirected to `MenuType.None` when gate is disabled, preventing dead-path opening from other callers.

What is left:
- Optional: if the team later wants on-device dev testing, gate can be expanded to include a scripting define in addition to `UNITY_EDITOR`.
