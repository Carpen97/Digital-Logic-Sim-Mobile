# Pin Order Bug Fix - Test Vector Generation

## Problem Summary

When generating test vectors using the 'G' key in a level, the test vectors were **failing validation** even though the circuit was correct. This happened because of a **pin ordering mismatch** between:

1. The **level definition's expected pin order** (defined in `levels.json`)
2. The **circuit's actual pin order** (the order pins were created/stored)

## Root Cause

### How Pin Order Works

1. **Level Definition** (`levels.json`):
   - Defines the expected order of pins
   - Example for 4-bit Subtractor: `B0, B1, B2, B3, A0, A1, A2, A3`

2. **Circuit Pins** (created by user):
   - Stored in `DevChipInstance.Elements` list
   - Returned by `GetInputPins()` in **whatever order they were added**
   - Could be different from the level definition if:
     - User deleted and recreated pins in a different order
     - Loaded a saved circuit with pins in a different order
     - Unity's internal ordering changed

3. **Test Vector Generator**:
   - Used `GetInputPins()` directly → **wrong order**
   - Generated test vectors with bits in wrong positions

### Example of the Bug

**Level expects:** `B0, B1, B2, B3, A0, A1, A2, A3`  
**Circuit has:** `A0, A1, A2, A3, B0, B1, B2, B3` (user created A pins first)

When generating test vectors:
- Test vector `"00001111"` in circuit means: A=0, B=15
- But level expects `"00001111"` to mean: B=0, A=15
- **Result:** All test vectors are wrong! ❌

## The Fix

### Changes Made to `LevelManager.cs`

Added three new helper methods:

#### 1. `SortPinsByLevelOrder()`
Sorts circuit pins to match the level definition's expected order by comparing pin names.

```csharp
private DevPinInstance[] SortPinsByLevelOrder(
    IEnumerable<DevPinInstance> circuitPins, 
    LevelDefinition.PinLabel[] expectedOrder)
```

#### 2. `ApplyInputsInOrder()`
Applies input values to pins in a specific order (not the default `GetInputPins()` order).

```csharp
private void ApplyInputsInOrder(BitVector inputVector, DevPinInstance[] orderedPins)
```

#### 3. `ReadOutputsInOrder()`
Reads output values from pins in a specific order (not the default `GetOutputPins()` order).

```csharp
private BitVector ReadOutputsInOrder(DevPinInstance[] orderedPins)
```

### Modified `GenerateTestVectors()`

Before generating test vectors, the method now:

1. Gets circuit pins using `GetInputPins()` and `GetOutputPins()`
2. **Sorts them** to match the level definition's expected order
3. Uses the **sorted pins** when applying inputs and reading outputs
4. Logs the sorted pin order for debugging

## Verification Steps

To verify the fix works:

1. Open a level (e.g., 4-bit Subtractor)
2. Build a **correct** circuit
3. Press **'G'** to generate test vectors
4. Check the console logs - you should see:
   ```
   [LevelManager] Sorted input pins: [B0, B1, B2, B3, A0, A1, A2, A3]
   [LevelManager] Sorted output pins: [Borrow, Difference 0, Difference 1, Difference 2, Difference 3]
   ```
5. The generated test vectors will be copied to clipboard
6. Paste them into `levels.json`
7. Test the level - it should **pass validation** ✅

## Impact

This fix ensures that:
- ✅ Test vector generation always uses the correct pin order
- ✅ Generated test vectors match the level definition's expectations
- ✅ Test vectors work regardless of the order pins were created in the circuit
- ✅ Validation will pass for correctly built circuits

## Future Considerations

This fix addresses the immediate pin ordering issue, but there are potential improvements:

1. **Pin Name Validation**: Add a warning if circuit pin names don't exactly match level definition
2. **Visual Feedback**: Show pin order in the UI when generating test vectors
3. **Automatic Pin Sorting**: Consider auto-sorting pins when starting a level to prevent issues
4. **Documentation**: Add guidelines for level creators about pin naming conventions

## Files Modified

- `Assets/Scripts/LevelsIntegration/LevelManager.cs`
  - Modified: `GenerateTestVectors()` method
  - Added: `SortPinsByLevelOrder()` method
  - Added: `ApplyInputsInOrder()` method
  - Added: `ReadOutputsInOrder()` method

## Testing

Recommended test cases:

1. **Normal case**: Generate test vectors with pins in correct order
2. **Reversed pins**: Delete and recreate pins in reverse order, then generate
3. **Multi-bit pins**: Test with levels that have multi-bit pins (like 8-bit adder)
4. **Missing pins**: What happens if a pin is missing? (Should log warning)
5. **Extra pins**: What happens if there are extra pins? (Should log warning)

## Date
October 21, 2025

