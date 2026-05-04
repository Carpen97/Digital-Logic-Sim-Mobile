## Status
Done

## Summary
Implemented a per-wire static color option so selected wires can be rendered with a fixed color instead of live signal color, eliminating flicker for clock/high-frequency wires. The setting persists through save/load, and simulation behavior is unchanged.

## What I did
- Added a per-wire flag in `WireInstance`:
  - `UseStaticColor` boolean.
  - Saved-wire constructor now accepts this value (default remains dynamic for new wires).
  - `GetColour()` now returns a fixed neutral/theme-disconnected color when enabled.
- Updated wire context menu in `ContextMenu`:
  - Added wire menu entry: `Static color`.
  - Added toggle action to flip `UseStaticColor` on the selected wire.
- Persisted setting in serialization:
  - Added `UseStaticColor` to `WireDescription`.
  - Included field in `DescriptionCreator.CreateWireDescription(...)`.
  - Included field in group serialization helper `CreateWireDescriptionWithIndexMap(...)`.
  - Loaded field in `DevChipInstance.TryLoadWireFromDescription(...)` when constructing `WireInstance`.
- Verified edited files with lints: no linter errors reported.
