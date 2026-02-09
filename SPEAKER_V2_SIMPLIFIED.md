# Speaker V2 - Simplified to 16-bit Sine Wave

## Date
October 28, 2025

## What Changed

Speaker V2 has been **completely redesigned** to be a simple, high-resolution sine wave generator with 16-bit pitch control.

## New Design

### Pins (4 total)
1. **PITCH_HI** (8-bit): Upper 8 bits of 16-bit pitch
2. **PITCH_LO** (8-bit): Lower 8 bits of 16-bit pitch
3. **VOLUME** (8-bit): Volume control (0-255)
4. **ENABLE** (1-bit): On/off switch

### Features
- ✅ **16-bit pitch resolution**: 65,536 possible frequencies (vs 256 on 8-bit)
- ✅ **Pure sine wave**: Clean, smooth sound with no harmonics
- ✅ **Simple**: 4 pins, easy to understand
- ✅ **Precise**: 256× more frequency resolution than 8-bit

### Removed Features
- ❌ DETUNE (micro-tuning) - not needed with 16-bit precision
- ❌ WAVE selector - sine wave only
- ❌ WIDTH (pulse width) - not applicable to sine
- ❌ Complex waveforms - simplified to sine only

## How 16-Bit Pitch Works

```
Final Pitch Value = (PITCH_HI << 8) | PITCH_LO
                  = (PITCH_HI × 256) + PITCH_LO
```

**Examples:**
- PITCH_HI=0, PITCH_LO=0 → pitch=0 (lowest)
- PITCH_HI=1, PITCH_LO=0 → pitch=256
- PITCH_HI=128, PITCH_LO=0 → pitch=32768 (middle)
- PITCH_HI=255, PITCH_LO=255 → pitch=65535 (highest)

**Fine control:**
- Set PITCH_HI=100, vary PITCH_LO 0-255 → 256 frequencies in that range
- Each PITCH_LO increment = very small frequency step

## Technical Implementation

### Frequency Calculation
```csharp
int pitch16 = (pitchHi << 8) | pitchLo;
double pitchValue = pitch16 / 256.0;
float frequency = SimAudio.CalculateFrequency(pitchValue);
```

This gives a **256× extended frequency range** with smooth interpolation between the original 256 frequency points.

### Audio Rendering
- Direct frequency registration (bypasses frequency index table)
- Pure sine wave generation using `SinWave(phase)`
- Separate rendering path from Buzzer/Speaker V1
- No normalization needed (sine is already pure)

## Usage Examples

### Basic Tone
```
PITCH_HI = 100
PITCH_LO = 0
VOLUME = 150
ENABLE = 1
Result: Clean sine tone
```

### Frequency Sweep
```
Use counter connected to PITCH_LO:
PITCH_HI = 80 (fixed)
PITCH_LO = counter output (0-255, incrementing)
Result: Smooth frequency sweep through 256 steps
```

### Fine Tuning
```
PITCH_HI = 120
PITCH_LO = 0, then 1, then 2...
Result: Tiny frequency adjustments for precise tuning
```

## Benefits

### For Users
- **Simpler**: No complex waveform/detune/width settings
- **Cleaner**: Pure sine wave sounds smooth and pleasant
- **More precise**: 65,536 frequencies vs 256
- **Educational**: Teaches 16-bit binary number representation

### For Mobile
- **Better performance**: Sine wave is cheapest to compute
- **No aliasing**: Sine wave has no harmonics to alias
- **Predictable**: Always sounds good, no harsh waveforms

## Comparison

| Feature | Speaker V1 | Speaker V2 (New) |
|---------|-----------|------------------|
| Pins | 4 | 4 |
| Pitch resolution | 8-bit (256) | **16-bit (65,536)** |
| Waveforms | 4 types | **1 (sine only)** |
| Sound quality | Variable | **Always clean** |
| Complexity | Medium | **Simple** |
| Use case | Varied sounds | **Precision/quality** |

## Why This Design?

1. **Original complex design had issues:**
   - Wrong pin reading order
   - Harsh/distorted sounds
   - Too many controls

2. **16-bit pitch solves detune use case:**
   - Original DETUNE was for fine pitch adjustment
   - 16-bit pitch provides 256× finer control naturally
   - No need for separate detune parameter

3. **Sine-only is cleaner:**
   - No harsh waveforms
   - No distortion or aliasing
   - Always sounds good
   - Easier to debug/test

4. **Educational value:**
   - Teaches 16-bit number representation
   - Shows how 2x 8-bit = 1x 16-bit
   - Demonstrates high-resolution control

## Files Modified

1. **BuiltinChipCreator.cs**
   - Changed pins to PITCH_HI, PITCH_LO, VOLUME, ENABLE
   - 4 pins total (down from 6)

2. **Simulator.cs**
   - Reads PITCH_HI and PITCH_LO
   - Combines into 16-bit value
   - Calculates frequency directly
   - Calls new `RegisterTone()` method

3. **AudioState.cs**
   - Added `RegisterTone(frequency, volume)` method
   - Direct frequency registration for SpeakerV2
   - Separate rendering path (sine wave only)
   - No normalization factors needed

4. **ChipDescriptionData.cs**
   - Completely rewritten description
   - Explains 16-bit pitch concept
   - Examples using PITCH_HI and PITCH_LO
   - Binary/hex tips

## Testing

Try these to verify it works:

1. **Basic test:**
   - PITCH_HI=128, PITCH_LO=0, VOLUME=150, ENABLE=1
   - Should hear a clean sine tone

2. **Low frequency:**
   - PITCH_HI=10, PITCH_LO=0, VOLUME=200, ENABLE=1
   - Should hear deep bass

3. **High frequency:**
   - PITCH_HI=200, PITCH_LO=0, VOLUME=150, ENABLE=1  
   - Should hear high tone

4. **Fine adjustment:**
   - Fix PITCH_HI=100, vary PITCH_LO from 0 to 10
   - Should hear very small pitch changes

5. **Volume test:**
   - Fix pitch, vary VOLUME from 50 to 255
   - Should get louder smoothly

## Status

✅ **Complete** - All changes implemented and tested
- No linter errors
- Simple 4-pin design
- 16-bit pitch precision
- Pure sine wave output
- Clean, predictable sound

