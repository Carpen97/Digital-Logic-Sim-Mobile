# Ticket 034: Code Review Guide

## Quick Review Checklist

### Files Changed
- ✅ `Assets/Scripts/Graphics/UI/MobileUIController.cs` - Main implementation
- ✅ `Assets/Scripts/Graphics/UI/Menus/ContextMenu.cs` - Helper method added

### Key Changes Overview

#### 1. MobileUIController.OnWrenchButtonPress() (Line 359-377)
```csharp
// OLD CODE:
isWrenchToolActive = !isWrenchToolActive;
// ... toggle logic

// NEW CODE:
// Check if exactly one editable component is selected and auto-open its edit menu
if (!isWrenchToolActive && TryAutoOpenEditMenuForSingleSelection())
{
    // Edit menu opened, don't activate wrench tool mode
    return;
}

// Default behavior: toggle wrench tool mode
isWrenchToolActive = !isWrenchToolActive;
// ... toggle logic
```

**Review Points:**
- ✅ Only attempts auto-open when wrench is not already active
- ✅ Falls back to default behavior if auto-open fails
- ✅ Minimal changes to existing code
- ✅ No breaking changes

#### 2. New Helper Methods (Lines 379-473)

**TryAutoOpenEditMenuForSingleSelection()** (Lines 383-410)
- Checks if exactly one element is selected
- Routes to appropriate handler based on type
- Returns true if menu opened, false otherwise

**TryAutoOpenChipEditMenu()** (Lines 412-458)
- Handles SubChipInstance types
- Sets interaction context before opening menu
- Switch statement for different chip types
- Returns false for LED/Button (intentional - they use context menu)

**TryAutoOpenPinEditMenu()** (Lines 460-473)
- Handles DevPinInstance types
- Sets interaction context and calls PinEditMenu.SetTargetPin()
- Opens rename menu

**Review Points:**
- ✅ Clear method names with XML documentation
- ✅ Proper null checks
- ✅ Permission checks (CanEditViewedChip)
- ✅ Follows existing patterns (UIDrawer.SetActiveMenu)

#### 3. ContextMenu.SetInteractionContext() (Lines 528-543)

```csharp
/// <summary>
/// Sets the interaction context for use by edit menus (ROM, Key, Pulse, Constant, etc).
/// This is needed when auto-opening edit menus from the wrench tool.
/// </summary>
public static void SetInteractionContext(IInteractable context)
{
    interactionContext = context;
    if (context is SubChipInstance subChip)
    {
        interactionContextName = subChip.Description.Name;
    }
    else if (context is DevPinInstance devPin)
    {
        interactionContextName = devPin.Name;
    }
}
```

**Review Points:**
- ✅ Public static method for external access
- ✅ Sets both context and contextName (needed by edit menus)
- ✅ Handles both SubChipInstance and DevPinInstance
- ✅ XML documentation explains purpose

---

## Architecture Decisions

### Why Add SetInteractionContext()?
- Edit menus (ROM, Key, Pulse, Constant) access selected chip via `ContextMenu.interactionContext`
- This is a static variable set by the context menu system when right-clicking
- Auto-open needs to set this context without opening the context menu
- New public method provides clean API for this use case

### Why Return False for LED/Button?
- LED and Button chips use color picker UI
- Color picker is currently integrated into context menu
- Auto-opening would bypass the color selection UI
- Future enhancement could implement dedicated color picker auto-open

### Why Check isWrenchToolActive?
- If wrench is already active, pressing it again should deactivate it
- Auto-open should only happen on the first press (activation)
- This maintains expected toggle behavior

---

## Testing Strategy

### Unit Test Scenarios (Manual)
1. **Single editable component** → Edit menu opens
2. **Multiple components** → Wrench mode activates
3. **No components** → Wrench mode activates
4. **Wrench already active** → Wrench deactivates

### Integration Test Scenarios
1. **ROM chip** → ROM edit menu shows correct data
2. **Key chip** → Key rebind menu shows current bindings
3. **Custom chip** → Chip opens in editor
4. **Dev pin** → Rename menu shows correct pin

### Edge Cases
1. **Cannot edit chip** → Falls back to wrench mode
2. **Null project** → No crash, returns false
3. **Null controller** → No crash, returns false

---

## Potential Issues & Mitigations

### Issue: Context not set correctly
**Mitigation:** SetInteractionContext() handles both SubChipInstance and DevPinInstance explicitly

### Issue: Menu opens but shows wrong data
**Mitigation:** Context is set immediately before opening menu, same pattern as context menu system

### Issue: Breaking existing wrench behavior
**Mitigation:** All existing paths preserved, new logic only adds early return on success

### Issue: Platform differences
**Mitigation:** Code works on all platforms, uses existing `#if UNITY_ANDROID || UNITY_IOS` checks where needed

---

## Performance Impact

- **Minimal:** Additional null checks and Count check on wrench button press
- **No loop overhead:** Single element check is O(1)
- **No allocations:** Uses existing selected elements list
- **UI thread:** All operations already on UI thread

---

## Backward Compatibility

✅ **Fully backward compatible**
- Existing wrench behavior unchanged for multi-selection
- Existing wrench behavior unchanged for no selection
- Context menu still works for right-click access
- No API changes to public interfaces (except new optional method)

---

## Code Style

- ✅ Follows C# naming conventions
- ✅ Uses existing code patterns (UIDrawer.SetActiveMenu, etc.)
- ✅ XML documentation on public methods
- ✅ Consistent indentation and formatting
- ✅ Clear variable names (no abbreviations)

---

## Recommended Review Focus

1. **Logic flow in OnWrenchButtonPress()** - Ensure early return doesn't skip critical code
2. **SetInteractionContext() usage** - Verify it's set before each menu open
3. **Permission checks** - Confirm CanEditViewedChip is checked appropriately
4. **Switch statement completeness** - All editable chip types covered?
5. **LED/Button behavior** - Intentional skip or oversight? (Intentional)

---

## Questions for Reviewer

1. Should LED/Button also auto-open with a simplified color picker? (Future enhancement)
2. Should there be a user preference to disable auto-open? (Future enhancement)
3. Is the current context menu behavior for LED/Button sufficient?
4. Any platform-specific concerns for mobile vs PC?

---

## Approval Criteria

- [ ] Code compiles without errors
- [ ] No linter warnings
- [ ] Manual testing confirms all success criteria
- [ ] No regressions in existing wrench tool behavior
- [ ] Code style matches project conventions
- [ ] Architecture decisions are sound

---

**Reviewer Signature:** _________________________  
**Date:** _________________________  
**Approved:** ☐ Yes  ☐ Yes with changes  ☐ No (see comments)

