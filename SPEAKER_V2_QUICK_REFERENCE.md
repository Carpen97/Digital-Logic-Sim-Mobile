# Speaker V2 - Quick Reference Guide

## Pin Configuration

```
┌─────────────┐
│  SPEAKER V2 │
├─────────────┤
PITCH    ──┤ (8-bit)  Main frequency (0-255)
DETUNE   ──┤ (4-bit)  Micro-tuning offset (0-15, centered at 8)
VOLUME   ──┤ (8-bit)  Loudness (0-255)
WAVE     ──┤ (3-bit)  Waveform selector (0-7)
WIDTH    ──┤ (4-bit)  Pulse duty cycle (0-15)
ENABLE   ──┤ (1-bit)  On/Off switch
└─────────────┘
```

## Waveforms (WAVE 0-7)

| WAVE | Name | Sound Character | Best For |
|------|------|-----------------|----------|
| 0 | Sine | Pure, smooth, flute-like | Mellow tones, sub-bass |
| 1 | Square | Harsh, hollow, 8-bit game | Retro game music, leads |
| 2 | Sawtooth | Bright, buzzy, synthesizer | Synth leads, brass |
| 3 | Triangle | Soft, mellow, gentle | NES-style music, pads |
| 4 | Pulse | Variable, rich harmonics | Analog synth, bass |
| 5 | Inverse Saw | Dark, falling, dramatic | Special effects, tension |
| 6 | Noise | Random static, chaotic | Drums, percussion, wind |
| 7 | Reserved | (Future expansion) | - |

## Detune Guide (DETUNE 0-15)

**DETUNE is centered at 8 (neutral/no change)**

| DETUNE | Effect | Use Case |
|--------|--------|----------|
| 0 | -8 steps (very flat) | Wide chorus, dramatic detune |
| 4 | -4 steps (flat) | Moderate chorus effect |
| 6-7 | -2 to -1 steps | Subtle chorus (slightly flat) |
| **8** | **No change (centered)** | **Standard tuning** |
| 9-10 | +1 to +2 steps | Subtle chorus (slightly sharp) |
| 12 | +4 steps (sharp) | Moderate chorus effect |
| 15 | +7 steps (very sharp) | Wide chorus, dramatic detune |

**Tip:** For chorus effects, use multiple speakers with DETUNE values around 8 (e.g., 7 and 9, or 6 and 10)

## Pulse Width Guide (WIDTH 0-15)

**Only affects WAVE=4 (Pulse)**

| WIDTH | Duty Cycle | Sound Character |
|-------|------------|-----------------|
| 0-2 | Narrow (0-13%) | Thin, nasal, reedy |
| 3-5 | Medium-narrow (20-33%) | Clarinet-like |
| 6-9 | Medium (40-60%) | Square-like, full |
| 10-12 | Medium-wide (67-80%) | Warm, thick |
| 13-15 | Wide (87-100%) | Hollow, oboe-like |

**Tip:** WIDTH=7 or 8 ≈ 50% = classic square wave sound

## Pitch Control

**PITCH (0-255) covers the full frequency range**

- Use PITCH for main frequency selection
- Higher values = higher pitch
- Full 8-bit range same as Speaker V1 and Buzzer
- DETUNE adds ±8 steps for fine adjustments

**Example frequencies:**
- PITCH = 30: Very low bass
- PITCH = 64: Low-mid range
- PITCH = 128: Middle range
- PITCH = 192: High range
- PITCH = 230: Very high range

**For chorus effects:**
- Set same PITCH on multiple speakers
- Vary DETUNE (e.g., 7, 8, 9) for thickness

## Quick Recipes

### 1. Clean Tone
```
PITCH = 128
DETUNE = 8 (neutral)
VOLUME = 180
WAVE = 0 (sine)
WIDTH = 0 (not used)
ENABLE = 1
```

### 2. Retro Game Beep
```
PITCH = 150
DETUNE = 8
VOLUME = 200
WAVE = 1 (square)
WIDTH = 0
ENABLE = 1
```

### 3. Deep Bass Throb
```
PITCH = 30
DETUNE = 8
VOLUME = 255
WAVE = 4 (pulse)
WIDTH = 3 (narrow)
ENABLE = 1
```

### 4. Synth Lead (Single)
```
PITCH = 128
DETUNE = 8
VOLUME = 220
WAVE = 2 (saw)
WIDTH = 0
ENABLE = 1
```

### 5. Chorused Synth Lead (2 Speakers)
```
Speaker 1: PITCH=128, DETUNE=7, VOLUME=200, WAVE=2
Speaker 2: PITCH=128, DETUNE=9, VOLUME=200, WAVE=2
Result: Thick, chorused lead sound
```

### 6. Super-Saw (3 Speakers)
```
Speaker 1: PITCH=128, DETUNE=6, VOLUME=180, WAVE=2
Speaker 2: PITCH=128, DETUNE=8, VOLUME=180, WAVE=2
Speaker 3: PITCH=128, DETUNE=10, VOLUME=180, WAVE=2
Result: Massive, wide synth sound
```

### 7. Hi-Hat
```
PITCH = 220
DETUNE = 8
VOLUME = 120
WAVE = 6 (noise)
WIDTH = 0
ENABLE = 1 (pulse briefly)
```

### 8. Snare Drum
```
PITCH = 130
DETUNE = 8
VOLUME = 180
WAVE = 6 (noise)
WIDTH = 0
ENABLE = 1 (pulse briefly)
```

### 9. Analog-style Bass
```
PITCH = 40-60 (vary for melody)
DETUNE = 8
VOLUME = 240
WAVE = 4 (pulse)
WIDTH = 10 (wide pulse)
ENABLE = 1
```

## Creating Chorus/Detune Effects (Use 2-3 Speaker V2 Chips)

### Subtle Chorus
- **Speaker 1:** PITCH=128, DETUNE=7, VOLUME=200
- **Speaker 2:** PITCH=128, DETUNE=9, VOLUME=200
- Result: Slight pitch difference creates warm chorus

### Wide Chorus
- **Speaker 1:** PITCH=128, DETUNE=5, VOLUME=180
- **Speaker 2:** PITCH=128, DETUNE=11, VOLUME=180
- Result: More pronounced chorus effect

### Super Unison (3 Voices)
- **Speaker 1:** PITCH=128, DETUNE=6, VOLUME=170
- **Speaker 2:** PITCH=128, DETUNE=8, VOLUME=170
- **Speaker 3:** PITCH=128, DETUNE=10, VOLUME=170
- Result: Thick, professional synth sound

## Tips & Tricks

### Sound Design
- **Always set DETUNE=8 for standard pitch** (no offset)
- Pulse wave + narrow WIDTH = rich, analog character
- Use noise at different PITCH for varied percussion
- Modulate WIDTH over time for evolving sounds

### Chorus Effects
- Use 2-3 speakers at same PITCH, different DETUNE
- DETUNE values 6-10 give subtle to moderate chorus
- DETUNE values 0-15 give extreme chorus/detune
- Great for making sounds fuller and more interesting

### Volume Control
- All waveforms are volume-normalized
- Start with VOLUME=150-200 for testing
- VOLUME=255 for maximum impact
- Use lower volumes for background sounds

### Performance
- Only enable speakers when needed (ENABLE=0 when silent)
- Use VOLUME=0 instead of rapid enable/disable
- Multiple speakers work fine on mobile

## Troubleshooting

| Problem | Solution |
|---------|----------|
| No sound | Check ENABLE=1, VOLUME>0 |
| Pitch too high/low | Adjust PITCH, set DETUNE=8 |
| Unwanted detune | Set DETUNE=8 (centered) |
| Too quiet | Increase VOLUME |
| Too loud | Decrease VOLUME (normalized) |
| Harsh sound | Try different WAVE type |
| WIDTH not working | Use WAVE=4 (pulse) |

## Differences from Speaker V1

| Feature | Speaker V1 | Speaker V2 |
|---------|-----------|------------|
| Pins | 4 | 6 |
| Detune control | ❌ | ✅ DETUNE pin |
| Waveforms | 4 | 8 |
| Pulse width | ❌ | ✅ WIDTH pin |
| Noise | ❌ | ✅ WAVE=6 |
| Chorus effects | Limited | Excellent |

## When to Use Which Speaker

- **Buzzer**: Learning basics, simple beeps
- **Speaker V1**: Wave types, intermediate sound design
- **Speaker V2**: Music composition, professional synthesis

---

**Need more help?** Check the full chip description in the chip menu for detailed explanations and musical theory!

