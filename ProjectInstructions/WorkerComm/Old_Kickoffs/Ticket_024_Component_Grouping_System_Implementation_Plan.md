# Ticket 024: Component Grouping System — Implementation Plan

This document outlines the implementation plan for the Component Grouping System. Execute this plan only when instructed to implement.

---

## 1. Architecture Overview

### 1.1 Core Concept

- **Group**: A set of chips (SubChipInstance) tied together by a shared `GroupId`. No DevPins in groups.
- **Selection**: Clicking any member selects the entire group.
- **Movement**: Same as multi-selection — all members move together.
- **Saved group**: Serialized layout (chips + wires) stored in library; placed like a chip but spawns multiple chips as a group.

### 1.2 Data Flow

```
User selects chips → "Make group" → Assign GroupId to all
User selects group member → Select entire group (expand selection)
User picks "Save group" → Create GroupDescription → Save to disk → Add to library
User picks group from library → StartPlacingGroup → Spawn chips + wires → Assign GroupId
```

---

## 2. Data Model Changes

### 2.1 Group Membership on Elements

**File:** `SubChipInstance.cs` (and `DevPinInstance.cs` if DevPins can ever be grouped — *they cannot per ticket: only chips*)

Add to `IMoveable` interface or directly to `SubChipInstance`:

- **`int GroupId`** — 0 = no group; non-zero = group membership. Chips in the same group share the same ID.
- Use existing `IDGenerator`-style approach: generate unique GroupIds when creating a group (e.g. `IDGenerator.GenerateNewGroupId()`).

**Alternative:** Add `GroupId` only to `SubChipInstance` since groups contain only chips (no DevPins per the ticket wording). Keep it simple.

### 2.2 GroupDescription (New Type)

**New file:** `Assets/Scripts/Description/Types/GroupDescription.cs`

```csharp
public class GroupDescription
{
    public string Name;
    public string DLSVersion;
    public SubChipDescription[] SubChips;  // Chips with positions, rotations, etc.
    public WireDescription[] Wires;       // Wires between them (using PinAddress with owner IDs)
}
```

- Reuse `SubChipDescription` and `WireDescription` — they already support positions and pin addressing.
- No InputPins/OutputPins — groups are self-contained layouts.

### 2.3 ChipLibrary Extension

**File:** `ChipLibrary.cs`

- Add `List<GroupDescription> allGroups` (or `Dictionary<string, GroupDescription>` for lookup).
- Add `GetGroupDescription(string name)`, `HasGroup(string name)`, `NotifyGroupSaved(GroupDescription)`.
- **Unified library view:** ChipLibraryMenu and SearchPopup need to show both chips and groups in one list. Options:
  - **A)** Single list of `(string name, bool isGroup)` — merge `allChips` and `allGroups` by name when drawing.
  - **B)** Add `ILibraryItem` with `Name` and `IsGroup`; ChipLibrary holds `List<ILibraryItem>`.
  **Recommendation:** Option A — keep separate lists, merge at display time. Simpler and minimal changes.

### 2.4 Save/Load for Groups

**Files:** `Saver.cs`, `Loader.cs`, `SavePaths.cs`

- **Path:** `GetGroupFilePath(string groupName, string projectName)` → e.g. `Chips/GroupName.group.json` or `Groups/GroupName.json`.
- **Recommendation:** Use subfolder `Groups` under project path to keep chips and groups separate in filesystem.
- Add `SavePaths.GetGroupsPath(string projectName)` → `Path.Combine(GetProjectPath(projectName), "Groups")`.
- `Saver.SaveGroup(GroupDescription, string projectName)` — serialize via existing `Serializer` (reuse `ChipDescription` serialization if format compatible, or add `SerializeGroupDescription`).
- `Loader.LoadGroups(string projectName)` — load all `.json` in Groups folder, return `GroupDescription[]`.
- Project description: Add `string[] AllCustomGroupNames` (similar to `AllCustomChipNames`) if groups need to be listed in project meta, or derive from directory listing.

---

## 3. Selection Logic

**File:** `ChipInteractionController.cs`

### 3.1 Expand Selection on Click (Group-Aware Selection)

In `Select(IMoveable element, bool addToCurrentSelection)`:

- **Before adding element:** If `element` is `SubChipInstance` with `GroupId != 0`, and we are *not* in add-to-selection mode:
  - Instead of selecting just `element`, **select all elements** in `ActiveDevChip.Elements` that have the same `GroupId`.
  - Use `ClearSelection()` then add all group members.
- If `addToCurrentSelection` is true (multi-mode): keep current behavior (toggle single element in/out of selection).
- **Edge case:** When selecting a group, ensure wires between group members are considered for movement (already done — wires move if both endpoints are in selection).

### 3.2 Selection Box

- When using selection box, if the box includes at least one member of a group, consider selecting the whole group (ticket says "selecting any one chip selects entire group"). So: after selection box adds elements, for each added element with `GroupId != 0`, expand to include all group members.

---

## 4. Context Menu Changes

**File:** `ContextMenu.cs`

### 4.1 New Entries

- **Make group** — visible when: multiple SubChipInstances selected (no DevPins in selection), and `Project.CanEditViewedChip`.
- **Ungroup** — visible when: selection is non-empty and all selected elements share the same non-zero `GroupId`.
- **Save group** — visible when: same as Ungroup (group selected).

### 4.2 Integration Points

Current flow: `HandleOpenMenuInput` runs on right-click (or wrench tool on mobile). It uses `InteractionState.ElementUnderMouse` to decide what to show.

- **Multi-selection right-click:** When right-clicking and `ElementUnderMouse` is in `controller.SelectedElements`, and `SelectedElements` has multiple items (or a full group), we need a **selection context menu** with "Make group" or "Ungroup"/"Save group".
- **Implementation:** Add a branch: if `SelectedElements.Count > 1` and all are SubChipInstance (or all are in same group), show appropriate entries. For a single hovered chip in a group, show normal chip menu + Ungroup/Save group.

### 4.3 Menu Entry Arrays

- Add `entries_multiSelection` = [Make group].
- Add `entries_group` = [Ungroup, Save group] (append to existing chip entries or show as separate section).
- For chip menu when chip is in group: append `entries_group` to the existing `entries_customSubchip` etc.

### 4.4 Actions

- **MakeGroup:** Assign new GroupId to all `SelectedElements` that are SubChipInstance. Clear selection, then re-select them (so they’re now a group). Record undo.
- **Ungroup:** Set `GroupId = 0` on all selected elements. Record undo.
- **SaveGroup:** Open a name input popup (reuse `ChipSaveMenu`-style or create `GroupSaveMenu`); on confirm, create `GroupDescription` from selection, save to disk, add to library. Record undo for the save action (optional; save is typically a one-way operation).

### 4.5 Mobile

- Wrench tool already opens context menu centered (`OpenContextMenuCentered`). Extend so when multiple chips are selected (via selection box + wrench), the same "Make group" / "Ungroup" / "Save group" appear.
- Need to ensure wrench tool can trigger multi-selection context menu when appropriate.

---

## 5. Save Group Flow

### 5.1 Creating GroupDescription from Selection

**New helper:** `DescriptionCreator.CreateGroupDescription(string name, List<IMoveable> elements, List<WireInstance> wires)` or similar.

- Filter: only `SubChipInstance` in `elements`. Get wires from `ActiveDevChip.Wires` where both endpoints belong to selected elements.
- Create `SubChipDescription[]` using `DescriptionCreator.CreateSubChipDescription` for each.
- Create `WireDescription[]` using `DescriptionCreator.CreateWireDescription` for each wire.
- Build `GroupDescription` with `Name`, `SubChips`, `Wires`.

### 5.2 Name Input Popup

- Reuse pattern from `ChipSaveMenu` or `ChipLabelMenu`: show input field + Cancel/Save.
- Validate: name required, no duplicate with existing chip or group.
- **New menu type:** `UIDrawer.MenuType.GroupSavePopup` or reuse existing save popup with a parameter.

### 5.3 Undo

- Making group, ungrouping: `UndoController.RecordGroupAction` (new) or similar.
- Saving group: typically not undone (persists to disk).

---

## 6. Library Integration

### 6.1 Unified List

**Files:** `ChipLibraryMenu.cs`, `SearchPopup.cs`, `BottomBarUI.cs`, `ProjectDescription.cs`

- **StarredList:** Currently holds `StarredItem { Name, IsCollection }`. Extend to `IsGroup` or infer: if name exists in `chipLibrary.allGroups`, it’s a group.
- **Collections:** Do groups go in collections? Ticket says "same library" and "searchable". For simplicity, groups can appear in collections like chips (by name). If a collection references a name that’s a group, treat it as group when placing.
- **DrawStarredEntry / DrawCollectionEntry:** When drawing an item, check `chipLibrary.HasGroup(name)`. If true, draw a **group marker** (icon/badge) next to the name.
- **ChipActionButtons (USE, OPEN, DELETE):** For groups, OPEN doesn’t apply (can’t "open" a group for editing—it’s a template). USE = place; DELETE = delete from library. Adjust button visibility.

### 6.2 Group Marker

- Small icon or text badge like `[G]` or a distinct icon. Place near the name in list items.
- Style: subtle so it doesn’t clutter, but clear at a glance.

### 6.3 Search

- `SearchPopup` and any search in `ChipLibraryMenu`: include group names. `chipLibrary.allGroups` names should be searchable.
- Filtering: show both chips and groups in results.

---

## 7. Group Preview (Library)

**File:** `ChipLibraryMenu.cs` — `DrawChipPreview`

### 7.1 Branch for Groups

- When selected item is a group (`chipLibrary.TryGetGroupDescription(selectedName, out var groupDesc)`):
  - Call `DrawGroupPreview(groupDesc, panelContentBounds)` instead of `DrawChipPreview`.

### 7.2 DrawGroupPreview Implementation

- **Input:** `GroupDescription` (SubChips with positions, Wires).
- **Logic:**
  - Compute bounding box of all SubChips (positions).
  - Scale and center the layout to fit preview window (reuse scaling logic from `DrawChipPreview`).
  - For each SubChip: get `ChipDescription` from library by `subChip.Name`, draw mini chip at scaled position (reuse `DrawChipPreview`-style drawing: body, pins, text—or simplified rectangles).
  - Optionally draw wires as simplified lines between chip centers (or skip for simplicity in v1).
- **Reuse:** `UI_DrawChipBody`, `DrawChipPreviewPins`, etc. at smaller scale. May need a helper that draws a single chip at a given position/scale for use in both single-chip and group preview.

---

## 8. Placing a Saved Group

**File:** `ChipInteractionController.cs`

### 8.1 New Method

- `StartPlacingGroup(GroupDescription groupDesc, Vector2 position)` or extend `StartPlacing` to accept `GroupDescription`.
- **Steps:**
  1. Create SubChipInstances from `groupDesc.SubChips` (look up `ChipDescription` from library for each).
  2. Assign positions relative to `position` (group layout is stored in absolute or relative coords; use center-of-bounding-box as anchor).
  3. Add all to `ActiveDevChip.Elements`.
  4. Create `WireInstance`s from `groupDesc.Wires` (map PinAddress owner IDs to the new SubChip IDs — need ID mapping when creating elements).
  5. Generate a new `GroupId`, assign to all created SubChipInstances.
  6. Set `SelectedElements` = all new chips, call `StartPlacing(elements)` or equivalent to enter placement/move mode.
- **ID mapping:** When creating SubChips from GroupDescription, assign new IDs via `IDGenerator`. Build a map `oldID -> newID` so WireDescriptions can be translated (PinAddress uses PinOwnerID which is the SubChip ID).

### 8.2 Entry Points

- `ChipLibraryMenu` — when user clicks USE on a group: `project.controller.StartPlacingGroup(groupDesc, InputHelper.MousePosWorld)`.
- `SearchPopup` — similarly.
- `BottomBarUI` — when placing from starred/collection and item is group: call `StartPlacingGroup`.

### 8.3 Bus Pairs and Special Chips

- If group contains bus origin, need to spawn bus terminus as well (same as single chip placement). Check `ChipTypeHelper.IsBusOriginType` during group placement.
- Level restrictions: same as chips — if group contains disallowed chip, show message and abort.

---

## 9. Undo Support

**File:** `UndoController.cs`

- **MakeGroup:** Record before state (GroupIds of affected elements), after state (new GroupId).
- **Ungroup:** Record before state (GroupIds), after state (0).
- **Place group:** Treated as adding multiple elements + wires; existing undo for add element/wire should cover it if we use the same recording.

---

## 10. File Change Summary

| File | Changes |
|------|---------|
| `SubChipInstance.cs` | Add `GroupId` property |
| `IMoveable` (if interface) | Add `GroupId` (or only on SubChipInstance) |
| **New:** `GroupDescription.cs` | New type |
| `ChipLibrary.cs` | Add `allGroups`, `HasGroup`, `GetGroupDescription`, `NotifyGroupSaved`, `RemoveGroup` |
| `SavePaths.cs` | Add `GetGroupsPath` |
| `Saver.cs` | Add `SaveGroup` |
| `Loader.cs` | Add `LoadGroups`; project init loads groups |
| `Project.cs` / `ProjectDescription.cs` | Ensure groups are loaded; possibly `AllCustomGroupNames` |
| `ChipInteractionController.cs` | Group-aware `Select`; `StartPlacingGroup`; selection box group expansion |
| `ContextMenu.cs` | Add Make group, Ungroup, Save group; multi-selection / group context branches |
| `ChipLibraryMenu.cs` | Unified list (chips + groups); group marker; `DrawGroupPreview`; USE for groups calls `StartPlacingGroup` |
| `SearchPopup.cs` | Include groups in search; place group when selected |
| `BottomBarUI.cs` | Handle group in starred/collection placement |
| `UIDrawer.cs` | Add `GroupSavePopup` menu type if needed |
| **New (optional):** `GroupSaveMenu.cs` | Name input for saving group |
| `UndoController.cs` | Record MakeGroup / Ungroup |
| `DescriptionCreator.cs` | Add `CreateGroupDescription` |

---

## 11. Implementation Order

1. **Data model:** Add `GroupId` to `SubChipInstance`; create `GroupDescription`; extend ChipLibrary.
2. **Selection:** Group-aware `Select` and selection box expansion.
3. **Context menu:** Make group, Ungroup, Save group.
4. **Save/load:** Saver, Loader, paths for groups.
5. **Library UI:** Unified list, group marker, USE/place for groups.
6. **Placement:** `StartPlacingGroup` with wire and ID mapping.
7. **Preview:** `DrawGroupPreview` in ChipLibraryMenu.
8. **Undo:** MakeGroup/Ungroup recording.
9. **Mobile:** Verify wrench tool and touch flows for multi-select and group actions.
10. **Testing:** Create group, move, ungroup, save, place from library, verify no regressions.

---

## 12. Edge Cases and Notes

- **DevPins in selection:** "Make group" only applies when selection is SubChipInstances. If DevPins are selected, don’t show Make group.
- **Mixed selection:** Selection with both grouped and ungrouped chips — "Make group" would create a new group containing all selected chips (including those already in other groups—they leave their old group and join the new one).
- **Nested groups:** Ticket doesn’t specify. Keep it flat: a chip belongs to at most one group. When creating a new group from a selection that includes chips from different groups, unify into one group.
- **Delete group member:** Deleting one chip in a group—should we remove it from group or delete the chip? Per ticket, "ungroup" dissolves the group. Delete removes the chip; it’s no longer in the group. No special handling needed.
- **Levels:** If the viewed chip is a level solution, ensure `CanEditViewedChip` gates group actions (already used in context menu).

---

## 13. Success Criteria Checklist

- [ ] User can select multiple chips and create a group via context menu
- [ ] Clicking one chip in a group selects the whole group; moving moves all
- [ ] User can ungroup via context menu
- [ ] User can save a group (with name) via context menu; saved groups appear in library with group marker and are searchable
- [ ] Library shows group preview (multi-chip mini view) when a saved group is selected
- [ ] Placing a saved group from library spawns all chips in layout as a group (grouped until user ungroups)
- [ ] No regressions: existing chip selection, movement, save chip, and library behaviour unchanged
- [ ] Mobile: same actions available (context menu or equivalent)

---

*End of Implementation Plan. Begin implementation when instructed.*
