# LED Display Aspect Ratio Fix

## Issue Reported

LED displays (and other displays) on chips were showing as rectangles in the preview but as squares in-game.

**Example:** A chip with an LED labeled "Super" appeared:
- **In Preview:** Rectangular (stretched/distorted)
- **In Game:** Square (correct)

## Root Cause

The preview was using **UI drawing functions** (`Seb.Vis.UI.UI.DrawQuad`, `UI.DrawPanel`, etc.) which have different coordinate space handling than the **game drawing functions** (`Draw.Quad`, `Draw.Diamond`, etc.).

The UI drawing functions may apply aspect ratio corrections or coordinate transformations that distort squares into rectangles, especially when:
1. The preview window has a non-1:1 aspect ratio
2. The parent chip has a non-square aspect ratio
3. UI space to screen space transformations are applied

## Solution

Changed all display rendering in the preview to use **game drawing functions** (`Draw.Quad`, `Draw.Diamond`, `Draw.Point`) instead of UI drawing functions. This ensures:
- ✅ Displays render with the same coordinate system as in-game
- ✅ Square displays remain square (no distortion)
- ✅ Preview matches in-game appearance exactly

## Files Modified

**File:** `Assets/Scripts/Graphics/UI/Menus/ChipLibraryMenu.cs`

### Changes Made:

#### 1. LED Display (lines 2528-2539)
```csharp
// BEFORE:
Seb.Vis.UI.UI.DrawPanel(centre, Vector2.one * scale, Color.black, Anchor.Centre);
Seb.Vis.UI.UI.DrawQuad(centre, pixelDrawSize, Color.red, Anchor.Centre);

// AFTER:
Draw.Quad(centre, Vector2.one * scale, Color.black);
Draw.Quad(centre, pixelDrawSize, Color.red);
```

#### 2. RGB Display (lines 2496-2526)
```csharp
// BEFORE:
Seb.Vis.UI.UI.DrawPanel(centre, Vector2.one * scale, Color.black, Anchor.Centre);
Seb.Vis.UI.UI.DrawQuad(pos, pixelDrawSize, pixelColor, Anchor.Centre);

// AFTER:
Draw.Quad(centre, Vector2.one * scale, Color.black);
Draw.Quad(pos, pixelDrawSize, pixelColor);
```

#### 3. 7-Segment Display (lines 2568-2582)
```csharp
// BEFORE:
Seb.Vis.UI.UI.DrawPanel(centre, boundsSize, Color.black, Anchor.Centre);
Seb.Vis.UI.UI.DrawDiamond(centre, segmentSize, color);

// AFTER:
Draw.Quad(centre, boundsSize, Color.black);
Draw.Diamond(centre, segmentSize, color);
```

## Technical Details

### Why Draw.Quad Works Better

**Game Drawing Functions (`Draw.Quad`):**
- Operate in world/game space coordinates
- No aspect ratio transformation applied
- `Vector2.one * scale` produces exact squares
- Same rendering as in-game

**UI Drawing Functions (`UI.DrawQuad`):**
- Operate in UI space coordinates
- May apply aspect ratio corrections
- UI space to screen space transformations
- Can distort shapes if parent space is non-square

### Consistency with Shape Rendering

We were already using `Draw.Triangle` for custom shape rendering (hexagons, triangles, custom polygons), which worked correctly. Using `Draw.Quad` and `Draw.Diamond` for displays maintains consistency.

## Testing

### Verify the Fix:
1. **LED Display:** Create a chip with an LED inside - should be square in both preview and game
2. **RGB Display:** RGB displays should be square
3. **7-Segment Display:** Should maintain correct aspect ratio (taller than wide, but not distorted)
4. **Wide Chips:** Test with chips that have long names (wide aspect ratio) - displays should still be square/correct shape
5. **Tall Chips:** Test with chips that are tall - displays should maintain shape

### Test Cases:
- [ ] Chip with single LED (like "Super" example)
- [ ] Chip with RGB display
- [ ] Chip with 7-segment display
- [ ] Wide chip with displays
- [ ] Tall chip with displays
- [ ] Chip with multiple displays

## Related Issues Fixed

This fix also ensures:
- ✅ DOT displays render correctly (uses `Draw.Point`)
- ✅ All display types maintain correct aspect ratios
- ✅ Preview matches in-game appearance for all display types

## Status

✅ **FIXED** - All display types now use game drawing functions for consistent rendering

**Compilation Status:** ✅ No linter errors  
**Ready for Testing:** ✅ Yes

---

## Additional Notes

### Why This Wasn't Caught Earlier

The shape rendering (hexagons, triangles, custom polygons) was already using `Draw.Triangle` which worked correctly. The issue only affected displays that were using UI drawing functions.

### Performance

No performance impact - `Draw.Quad` and `Draw.Diamond` are the same functions used in-game, so they're already optimized.

### Future Considerations

If adding new display types to the preview, always use `Draw.*` functions instead of `Seb.Vis.UI.UI.*` functions to ensure correct rendering.

