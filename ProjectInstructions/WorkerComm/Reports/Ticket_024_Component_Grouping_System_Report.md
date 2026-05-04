# Ticket 024: Component Grouping System — Report

**Status:** Done

**Summary:** Implemented the full component grouping system: users can select multiple chips (and input/output pins) and create a group via context menu; clicking one element in a group selects the whole group; groups can be ungrouped or saved to the library; saved groups appear in the library with a [G] marker, in the GROUPS collection, and can be placed like chips with ghost/drag placement. Group preview shows outlines, pins, text, and displays; input/output pins can be grouped the same as chips.

---

## What I Did

### Data model
- **SubChipInstance.cs:** Added `GroupId` property (0 = not in group).
- **GroupDescription.cs (new):** Serializable type with `Name`, `SubChipDescription[]`, `WireDescription[]`.
- **ChipLibrary.cs:** Added `allGroups`, `groupFromNameLookup`, `HasGroup`, `GetGroupDescription`, `TryGetGroupDescription`, `NotifyGroupSaved`, `RemoveGroup`, `GetAllGroupNames`.
- **IDGenerator.cs:** Added `GenerateNewGroupId()`.

### Save/load
- **SavePaths.cs:** Added `GetGroupsPath()`.
- **Saver.cs:** Added `SaveGroup()`, `DeleteGroup()`.
- **Loader.cs:** Added `LoadGroups()`; `LoadChipLibrary` now passes groups into `ChipLibrary`.
- **Serializer.cs:** Added `SerializeGroupDescription`, `DeserializeGroupDescription`.

### Selection
- **ChipInteractionController.cs:** Updated `Select()` to expand to full group when clicking a grouped chip (unless in add-to-selection mode). Added `ExpandSelectionToIncludeFullGroups()` for selection-box completion.

### Context menu
- **ContextMenu.cs:** Added "MAKE GROUP", "UNGROUP", "SAVE GROUP". Show "MAKE GROUP" when multiple chips selected; show "UNGROUP" and "SAVE GROUP" when a group is selected. Implemented `MakeGroup()`, `Ungroup()`, `SaveGroup()`.
- **GroupSaveMenu.cs (new):** Name-input popup for saving groups.
- **UIDrawer.cs:** Added `GroupSavePopup` menu type.

### Save group flow
- **DescriptionCreator.cs:** Added `CreateGroupDescription()` and `CreateWireDescriptionWithIndexMap()` for correct wire index mapping.
- **GroupSaveMenu.cs:** Validates name, creates `GroupDescription`, saves to disk, notifies library, stars the group.

### Placement
- **ChipInteractionController.cs:** Added `StartPlacingGroup()` — creates `SubChipInstance`s with new IDs, remaps wire addresses, loads wires, assigns `GroupId`, enters placement mode.

### Library UI
- **ChipLibraryMenu.cs:** `ChipActionButtons` handles groups (USE = place group, no OPEN, DELETE). Added `DrawChipOrGroupPreview()` and `DrawGroupPreview()`. Group marker `[G]` in starred list and collection chip lists.
- **SearchPopup.cs:** Included groups in search; `UseChip` calls `StartPlacingGroup` for groups; group marker in results.
- **BottomBarUI.cs:** `TryStartPlacing` calls `StartPlacingGroup` when item is a group.
- **Project.cs:** Added `DeleteGroup()`.

### Undo
- **UndoController.cs:** Added `GroupUndoAction`, `RecordMakeGroup()`, `RecordUngroup()`.

---

## Files Touched

| File | Change |
|------|--------|
| `SubChipInstance.cs` | Add `GroupId` |
| `GroupDescription.cs` | New |
| `ChipLibrary.cs` | Groups support |
| `IDGenerator.cs` | `GenerateNewGroupId` |
| `SavePaths.cs` | `GetGroupsPath` |
| `Saver.cs` | `SaveGroup`, `DeleteGroup` |
| `Loader.cs` | `LoadGroups` |
| `Serializer.cs` | Group serialization |
| `DescriptionCreator.cs` | `CreateGroupDescription`, wire index mapping |
| `ChipInteractionController.cs` | Group-aware Select, `StartPlacingGroup`, selection expansion |
| `ContextMenu.cs` | Make group, Ungroup, Save group |
| `GroupSaveMenu.cs` | New |
| `UIDrawer.cs` | `GroupSavePopup` |
| `ChipLibraryMenu.cs` | Group actions, preview, marker |
| `SearchPopup.cs` | Groups in search, `UseChip` for groups |
| `BottomBarUI.cs` | `StartPlacingGroup` for groups |
| `Project.cs` | `DeleteGroup` |
| `UndoController.cs` | `RecordMakeGroup`, `RecordUngroup` |
| `DevPinInstance.cs` | Add `GroupId` |
| `GroupDescription.cs` | Add `InputPins`, `OutputPins` |
| `BuiltinCollectionCreator.cs` | Add OTHER, GROUPS collections |

---

## Success Criteria Checklist

- [x] User can select multiple chips and create a group via context menu
- [x] Clicking one chip in a group selects the whole group; moving moves all
- [x] User can ungroup via context menu
- [x] User can save a group (with name) via context menu; saved groups appear in library with [G] marker and are searchable
- [x] Library shows group preview (multi-chip mini view) when a saved group is selected
- [x] Placing a saved group from library spawns all chips in layout as a group (grouped until user ungroups)
- [x] No regressions intended: existing chip flows unchanged
- [x] Mobile: same actions via wrench tool / context menu
- [x] Input/output pins can be grouped like chips

---

## Notes for Testing

1. **Make group:** Select 2+ chips (shift+click or selection box), right-click one → "Make group".
2. **Selection:** Click any chip in a group → entire group selected.
3. **Ungroup:** With group selected, right-click → "Ungroup".
4. **Save group:** With group selected, right-click → "Save group" → enter name → Save.
5. **Place group:** Open library, find saved group (marked [G]), click USE.
6. **Search:** Search by group name; groups appear with [G]; USE places the group.
7. **Undo:** Make group / Ungroup should be undoable.

Groups are stored under `Projects/<ProjectName>/Groups/*.json`.

---

## Post-implementation Updates

### Bug fixes & enhancements
- **Context menu:** When group pre-selected, right-click showed "Make group" instead of "Ungroup/Save group". Fixed by checking `CanUngroup()` first before `CanMakeGroup()`.
- **Save group:** Save did nothing — `CancelEverything` on menu open cleared selection. Fixed by capturing selected elements in `OnMenuOpened` before cancel runs.
- **Group placement:** Groups placed instantly instead of ghost/drag. Fixed by removing `CancelEverything` after populating selection; added `FinishPlacingNewElements` skip for already-placed elements; added cancel cleanup for groups.
- **Group preview:** Preview showed solid rectangles only. Fixed by reusing `UI_DrawChipOutline`, `DrawChipPreviewPins`, `UI_DrawChipBody`, `DrawChipPreviewText`, `DrawChipPreviewDisplays`, and `DrawDevPinStyleDisplay` per chip/pin in the group.
- **GROUPS collection:** Added standard "GROUPS" collection (like "OTHER"); new groups auto-added there on library open. Added to `BuiltinCollectionCreator.CreateDefaultChipCollections()`.
- **Input/output pins:** Extended grouping to `DevPinInstance`. Added `GroupId` to DevPinInstance; `GroupDescription` now has `InputPins`/`OutputPins`; full support in context menu, Select, ExpandSelectionToIncludeFullGroups, MakeGroup/Ungroup, save/load, `StartPlacingGroup`, group preview, undo.
