# ROM Pin Configuration Change Bug Fix

## Issue Description

When editing a ROM 256x16 chip in the ROM Edit Menu and changing the pin configuration from the default `2x8` to any other setting (`1x16`, `4x4`, or `16x1`), the following exception was thrown:

```
Exception: Failed to find subchip with id 169814812
DLS.Simulation.SimChip.GetSubChipFromID (System.Int32 id)
DLS.Game.Project.NotifyRomContentsEdited (DLS.Game.SubChipInstance romChip)
DLS.Graphics.RomEditMenu.SaveChangesToROM ()
```

## Root Cause Analysis

### The Problem Flow

1. **User confirms pin configuration change** → `HandlePinConfigurationChange()` is called
2. **Chip replacement occurs** → `ReplaceRomChip(newChipType)` is executed:
   - Calls `devChip.DeleteSubChip(romChip)` - **queues** a RemoveSubChip command
   - Calls `devChip.AddNewSubChip(newRomChip, false)` - **queues** an AddSubChip command
   - Updates local `romChip` reference to the new chip
3. **Immediate save attempt** → `SaveChangesToROM()` is called right after replacement
4. **Simulation query fails** → `Project.NotifyRomContentsEdited(romChip)` tries to find the chip by ID in the simulation

### Why It Failed

The Simulator uses a **concurrent modification queue** (`ConcurrentQueue<SimModifyCommand>`) for all chip operations:
- `AddSubChip()` and `RemoveSubChip()` enqueue commands, they don't execute immediately
- Commands are processed later by `Simulator.ApplyModifications()` on the simulation thread
- When `SaveChangesToROM()` tries to find the chip in the simulation, the queued operations haven't been processed yet
- The old chip might still exist, or it's been removed but the new one hasn't been added yet

### Key Code Evidence

From `Simulator.cs`:
```csharp
static readonly ConcurrentQueue<SimModifyCommand> modificationQueue = new();

public static void AddSubChip(SimChip simChip, ChipDescription desc, ...)
{
    SimModifyCommand command = new() { ... };
    modificationQueue.Enqueue(command);  // QUEUED, not executed!
}
```

From `DevChipInstance.cs`:
```csharp
public void DeleteSubChip(SubChipInstance subChip)
{
    DeleteWiresAttachedToElement(subChip.ID);
    RemoveElement(subChip);
    if (hasSimChip) Simulator.RemoveSubChip(SimChip, subChip.ID); // QUEUED!
}

public void AddNewSubChip(SubChipInstance subChip, bool isLoading)
{
    AddElement(subChip);
    if (!isLoading)
    {
        Simulator.AddSubChip(SimChip, subChip.Description, ...); // QUEUED!
    }
}
```

## Solution

**Don't call `SaveChangesToROM()` after a chip replacement** because:
1. The ROM data is already preserved during the replacement process (passed to `SubChipDescription` with `internalData`)
2. The simulation won't have the chip available until queued commands are processed
3. The data will be automatically synchronized when the simulation updates

### Implementation

Added a tracking flag `chipWasReplaced` to detect when a chip replacement has occurred:

1. **Added flag** (line 28):
   ```csharp
   static bool chipWasReplaced = false; // Track if chip was replaced
   ```

2. **Set flag when replacement occurs** (line 1056-1057):
   ```csharp
   ReplaceRomChip(newChipType);
   chipWasReplaced = true; // Mark that chip was replaced
   ```

3. **Skip save if replaced** (lines 232-237):
   ```csharp
   // Only save ROM contents if chip wasn't replaced
   // (replacement already preserves the data, and simulation isn't ready yet)
   if (!chipWasReplaced)
   {
       SaveChangesToROM();
   }
   ```

4. **Reset flag on menu open** (line 951):
   ```csharp
   chipWasReplaced = false; // Reset replacement flag
   ```

## Testing Recommendations

Test the following scenarios:
1. ✅ Change pin configuration from 2x8 to 1x16 (chip replacement)
2. ✅ Change pin configuration from 2x8 to 4x4 (chip replacement)
3. ✅ Change pin configuration from 2x8 to 16x1 (chip replacement)
4. ✅ Edit ROM data and confirm without changing pin configuration (normal save)
5. ✅ Change pin configuration when output wires exist (warning dialog path)
6. ✅ Verify ROM data is preserved after pin configuration change
7. ✅ Verify visual grouping updates correctly in graphical mode

## Files Modified

- `Assets/Scripts/Graphics/UI/Menus/RomEditMenu.cs`

## Related Components

- `DevChipInstance.cs` - Handles chip addition/removal
- `Simulator.cs` - Queues modification commands
- `SimChip.cs` - Throws the exception when chip not found
- `Project.cs` - Contains `NotifyRomContentsEdited()` method

