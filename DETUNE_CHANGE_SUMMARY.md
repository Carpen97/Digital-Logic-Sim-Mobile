# Speaker V2 - DETUNE Implementation Change

## Date
October 28, 2025

## What Changed

The **OCTAVE pin** has been renamed to **DETUNE** and its behavior has been completely changed from musical octaves to micro-tuning offsets.

## Before (Musical Octaves - Incorrect Expectation)

```csharp
// Old behavior
int basePitch = octave * 36;  // Each octave = 36 steps
int pitchOffset = pitch * 36 / 256;
int finalPitch = Clamp(basePitch + pitchOffset, 0, 255);
```

**Problem:**
- OCTAVE=0: indices 0-35
- OCTAVE=1: indices 36-71
- **OCTAVE=8 (MSB set): index 255 = SUPER HIGH PITCH!** 🔊
- User expected micro-tuning, not huge octave jumps
- Not intuitive for fine-tuning/chorus effects

## After (Micro-Tuning - Correct Implementation)

```csharp
// New behavior
int detuneOffset = (int)detune - 8;  // Center at 8
int finalPitch = Clamp((int)pitch + detuneOffset, 0, 255);
```

**Solution:**
- **DETUNE=8: No change** (centered, neutral position)
- DETUNE=0: -8 steps (slightly flat)
- DETUNE=7: -1 step (barely flat)
- DETUNE=9: +1 step (barely sharp)
- DETUNE=15: +7 steps (slightly sharp)

**Benefits:**
- ✅ Intuitive centered control (middle value = no change)
- ✅ Perfect for chorus effects (multiple speakers at DETUNE 7,8,9)
- ✅ MSB doesn't cause extreme pitch jump
- ✅ Fine-tuning for matching other audio sources
- ✅ Can create vibrato by modulating DETUNE

## Usage Examples

### Standard Pitch (No Detune)
```
PITCH = 128
DETUNE = 8    ← Centered, no offset
VOLUME = 200
WAVE = 0
```

### Subtle Chorus Effect (2 Speakers)
```
Speaker 1:
  PITCH = 128
  DETUNE = 7    ← Slightly flat (-1 step)
  VOLUME = 200
  WAVE = 2

Speaker 2:
  PITCH = 128
  DETUNE = 9    ← Slightly sharp (+1 step)
  VOLUME = 200
  WAVE = 2
  
Result: Rich, chorused sound!
```

### Wide Chorus (2 Speakers)
```
Speaker 1:
  PITCH = 128
  DETUNE = 5    ← More flat (-3 steps)
  VOLUME = 180
  WAVE = 2

Speaker 2:
  PITCH = 128
  DETUNE = 11   ← More sharp (+3 steps)
  VOLUME = 180
  WAVE = 2
  
Result: Wider, more pronounced chorus
```

### Super-Saw Effect (3 Speakers)
```
Speaker 1: PITCH=128, DETUNE=6, WAVE=2
Speaker 2: PITCH=128, DETUNE=8, WAVE=2
Speaker 3: PITCH=128, DETUNE=10, WAVE=2

Result: Massive, professional synth sound
```

## Why Centered at 8?

For a 4-bit value (0-15), the mathematical center is 7.5, so we chose 8:
- Gives ±8 steps range (0→-8, 15→+7)
- Middle value of 8 feels natural as "no change"
- Easy to remember: **8 = neutral**

## Files Changed

1. **Simulator.cs**
   - Changed calculation from octave-based to offset-based
   - Renamed variable from `octave` to `detune`

2. **BuiltinChipCreator.cs**
   - Renamed pin from "OCTAVE" to "DETUNE"

3. **ChipDescriptionData.cs**
   - Updated educational description
   - Changed examples to use DETUNE
   - Added chorus effect examples
   - Removed octave/musical theory content

4. **SPEAKER_V2_QUICK_REFERENCE.md**
   - Updated all documentation
   - Changed "Octave Guide" to "Detune Guide"
   - Updated all recipes to use DETUNE=8
   - Added chorus effect recipes

## Educational Value

The DETUNE parameter teaches:
1. **Micro-tuning** - Fine pitch adjustments
2. **Chorus effects** - How slight pitch differences create thickness
3. **Centered controls** - Middle value = neutral/off
4. **Signed offsets** - Positive and negative adjustments

## Quick Reference

| DETUNE | Offset | Description |
|--------|--------|-------------|
| 0 | -8 | Very flat |
| 4 | -4 | Moderately flat |
| 6 | -2 | Slightly flat |
| 7 | -1 | Barely flat |
| **8** | **0** | **No change (use this as default!)** |
| 9 | +1 | Barely sharp |
| 10 | +2 | Slightly sharp |
| 12 | +4 | Moderately sharp |
| 15 | +7 | Very sharp |

## Key Takeaway

**Always set DETUNE=8 for normal/standard pitch!**

Setting DETUNE to any other value will offset the pitch by a small amount, which is great for:
- Chorus effects (multiple speakers)
- Fine-tuning to match other audio
- Creating vibrato (modulate DETUNE over time)
- Making sounds thicker and more interesting

---

**Status:** ✅ Implemented and documented

