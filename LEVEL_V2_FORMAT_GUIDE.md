# Level V2 Format Guide

## 🎯 Overview

The V2 level format is a **cleaner, more intuitive** way to define levels. It runs **side-by-side** with the old V1 format, so you can migrate gradually.

---

## 📦 File Structure

Both V1 and V2 levels can coexist in the same chapter:

```json
{
  "chapters": [
    {
      "chapterId": "ch.basics",
      "chapterName": "Basics",
      "chapterDescription": "...",
      
      "levels": [/* V1 levels */],
      "levelsV2": [/* V2 levels */]
    }
  ]
}
```

---

## 🆕 V2 Format Structure

```json
{
  "id": "lvl.and.1",
  "name": "AND Gate",
  "chapterId": "ch.basics",
  "description": "Output should be 1 only if both inputs are 1.",
  
  "inputStructure": [
    { "name": "B", "abbr": "B" },
    { "name": "A", "abbr": "A" }
  ],
  
  "outputStructure": [
    { "name": "Y", "abbr": "Y" }
  ],
  
  "testsInline": [
    "0_0|0",
    "0_1|0",
    "1_0|0",
    "1_1|1"
  ]
}
```

---

## 📌 Pin Structure (`PinData`)

Each pin in `inputStructure` or `outputStructure` has:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `name` | string | ✅ Yes | - | Display name (e.g., "Data A") |
| `abbr` | string | ❌ No | `name` | Short abbreviation (e.g., "A") |
| `nBits` | int | ❌ No | `1` | Number of bits (for multi-bit pins) |
| `pos` | float[] | ❌ No | auto | Manual position override `[x, y]` |

### Examples

**Single-bit pin:**
```json
{ "name": "A" }
```
→ Defaults to: `abbr="A"`, `nBits=1`

**Multi-bit pin:**
```json
{ "name": "Data A", "abbr": "A", "nBits": 8 }
```

**With custom position:**
```json
{ "name": "Clock", "abbr": "CLK", "pos": [0, 5] }
```

---

## 🧪 Test Formats

### Option 1: Inline Tests (Simple)

Use the format: `INPUT|OUTPUT`
- Use `_` to separate pins
- `0` or `1` for each bit

**Example (2 single-bit inputs, 1 output):**
```json
"testsInline": [
  "0_0|0",
  "0_1|0",
  "1_0|0",
  "1_1|1"
]
```

**Example (8-bit inputs):**
```json
"testsInline": [
  "00000000_00000000|00000000_0",
  "00000001_00000001|00000010_0",
  "11111111_00000001|00000000_1"
]
```

The underscores are **optional** for readability. These are equivalent:
```json
"0_0|0"  ✅ Clear
"00|0"   ✅ Also valid
```

### Option 2: Binary File (Large Test Sets)

For levels with 100+ tests, use a binary `.tvec` file:

```json
"testsBinaryPath": "GeneratedTestVectors/lvl.adder4bit.1"
```

(No file extension needed - Unity Resources loads `.tvec` automatically)

---

## 🔄 How It Works

1. **JSON Parsing**: Unity loads `levels.json`
2. **Automatic Conversion**: V2 levels are converted to V1 format internally
3. **Validation**: All existing validation code works unchanged
4. **Pin Spawning**: Works with both formats

The conversion happens in `Chapter.GetAllLevelsAsV1()`:
- V1 levels pass through unchanged
- V2 levels → converted to V1 → validated → returned

---

## ✨ Benefits of V2 Format

### Old Format (V1)
```json
{
  "inputCount": 2,
  "outputCount": 1,
  "inputBitCounts": [1, 1],
  "outputBitCounts": [1],
  "inputPinLabels": [
    { "name": "B", "abbr": "B" },
    { "name": "A", "abbr": "A" }
  ],
  "outputPinLabels": [
    { "name": "Y", "abbr": "Y" }
  ],
  "testVectors": [
    { "inputs": "00", "expected": "0" }
  ]
}
```

### New Format (V2)
```json
{
  "inputStructure": [
    { "name": "B" },
    { "name": "A" }
  ],
  "outputStructure": [
    { "name": "Y" }
  ],
  "testsInline": [
    "0_0|0"
  ]
}
```

**Improvements:**
- ✅ **Less redundancy** - Pin count derived from array length
- ✅ **Unified structure** - All pin data in one place
- ✅ **Readable tests** - `0_0|0` vs `"inputs": "00", "expected": "0"`
- ✅ **Smart defaults** - `abbr` defaults to `name`, `nBits` defaults to 1

---

## 🧪 Testing Strategy

### Step 1: Add a V2 Duplicate
```json
{
  "levels": [
    { "id": "lvl.and.1", /* ... V1 format ... */ }
  ],
  "levelsV2": [
    { "id": "lvl.and.1.v2", /* ... V2 format ... */ }
  ]
}
```

### Step 2: Test Side-by-Side
1. Open the game
2. Play both levels
3. Verify they behave identically
4. Check console for conversion logs

### Step 3: Migrate
Once confident, remove the V1 version and rename V2:
```json
{
  "levelsV2": [
    { "id": "lvl.and.1", /* ... V2 format ... */ }
  ]
}
```

---

## 🐛 Debugging

### Console Logs

The system logs conversion events:
```
[Chapter] Converted V2 level 'lvl.and.1.v2' to V1 format
```

If validation fails:
```
[Chapter] Invalid V2 level 'lvl.and.1.v2': Input bit count mismatch...
```

### Common Issues

**1. Bit count mismatch**
```json
// ❌ Wrong (says 1 bit, but input is 8 bits)
"inputStructure": [{ "name": "A" }],
"testsInline": ["11111111|0"]

// ✅ Correct
"inputStructure": [{ "name": "A", "nBits": 8 }],
"testsInline": ["11111111|0"]
```

**2. Missing separator**
```json
// ❌ Wrong (no | separator)
"testsInline": ["000"]

// ✅ Correct
"testsInline": ["00|0"]
```

**3. Pin count mismatch**
```json
// ❌ Wrong (2 pins, but input has 3 bits separated)
"inputStructure": [{ "name": "A" }, { "name": "B" }],
"testsInline": ["0_0_0|0"]  // 3 sections!

// ✅ Correct (if you want 3 pins)
"inputStructure": [{ "name": "A" }, { "name": "B" }, { "name": "C" }],
"testsInline": ["0_0_0|0"]
```

---

## 📝 Examples

### Simple Gate
```json
{
  "id": "lvl.not.1",
  "name": "NOT Gate",
  "chapterId": "ch.basics",
  "description": "Output should be the inverse of input.",
  "inputStructure": [{ "name": "A" }],
  "outputStructure": [{ "name": "Y" }],
  "testsInline": ["0|1", "1|0"]
}
```

### Multi-bit Circuit
```json
{
  "id": "lvl.8bit.adder.1",
  "name": "8-Bit Adder",
  "chapterId": "ch.8bit",
  "description": "Add two 8-bit numbers.",
  "inputStructure": [
    { "name": "Data A", "abbr": "A", "nBits": 8 },
    { "name": "Data B", "abbr": "B", "nBits": 8 }
  ],
  "outputStructure": [
    { "name": "Sum", "abbr": "SUM", "nBits": 8 },
    { "name": "Carry", "abbr": "C" }
  ],
  "testsInline": [
    "00000000_00000000|00000000_0",
    "00000001_00000001|00000010_0"
  ]
}
```

### Using Binary Test File
```json
{
  "id": "lvl.4bit.adder.1",
  "name": "4-Bit Adder",
  "chapterId": "ch.arithmetic",
  "description": "Add two 4-bit numbers.",
  "inputStructure": [
    { "name": "A0" }, { "name": "A1" }, { "name": "A2" }, { "name": "A3" },
    { "name": "B0" }, { "name": "B1" }, { "name": "B2" }, { "name": "B3" }
  ],
  "outputStructure": [
    { "name": "S0" }, { "name": "S1" }, { "name": "S2" }, { "name": "S3" },
    { "name": "Carry Out", "abbr": "Co" }
  ],
  "testsBinaryPath": "GeneratedTestVectors/lvl.adder4bit.1"
}
```

---

## 🚀 Migration Checklist

- [ ] Backup `levels.json`
- [ ] Create V2 version of a simple level (e.g., NOT gate)
- [ ] Add to `levelsV2` array in same chapter
- [ ] Test in-game
- [ ] Verify console shows conversion log
- [ ] Compare behavior with V1 version
- [ ] Repeat for more levels
- [ ] Once confident, remove V1 versions

---

## 📚 Technical Details

### Files Modified
- `Assets/Scripts/Levels/PinData.cs` - New pin structure
- `Assets/Scripts/Levels/LevelDefinitionV2.cs` - New level format
- `Assets/Scripts/LevelsIntegration/LevelPack.cs` - Support both formats
- `Assets/Scripts/Graphics/UI/Menus/LevelsMenu.cs` - Load both formats
- `Assets/Scripts/Graphics/UI/Menus/HallOfFameMenu.cs` - Load both formats
- `Assets/Scripts/Game/Main/UnityMain.cs` - Load both formats

### Conversion Process
1. `LevelDefinitionV2.Validate()` - Check format is valid
2. `LevelDefinitionV2.ToV1()` - Convert to V1 structure
3. `LevelDefinitionV2.ParseInlineTests()` - Parse test strings

### Backwards Compatibility
- ✅ All existing V1 levels work unchanged
- ✅ No code changes needed in validation/simulation
- ✅ Can mix V1 and V2 in same file
- ✅ Can gradually migrate one level at a time

---

## 🎓 Best Practices

1. **Start small** - Migrate simple gates first (NOT, AND, OR)
2. **Use underscores** - Makes multi-bit tests readable: `11111111_00000000|10000000_1`
3. **Binary for large sets** - Use `.tvec` files for 50+ test vectors
4. **Test in pairs** - Keep V1 version alongside V2 until verified
5. **Check logs** - Watch console for conversion errors
6. **Validate early** - The system catches format errors on load

---

## ❓ FAQ

**Q: Can I mix V1 and V2 in the same chapter?**
A: Yes! The `levels` and `levelsV2` arrays are independent.

**Q: What happens if I have duplicate IDs?**
A: Both will load. Use different IDs (e.g., `lvl.and.1` vs `lvl.and.1.v2`) while testing.

**Q: Do I need to restart Unity when changing JSON?**
A: No, just reload the level menu (exit and re-enter).

**Q: Can I use sequential circuits in V2?**
A: Not yet - sequential support is planned for a future update.

**Q: What about hints?**
A: Removed for now to keep format minimal. May be added back later.

---

## 🔮 Future Enhancements

Planned for future versions:
- [ ] Sequential circuit support
- [ ] Hints field
- [ ] Difficulty ratings
- [ ] Prerequisites system
- [ ] Tags/categories
- [ ] Custom positioning for all pins

---

**Ready to migrate?** See `LEVEL_V2_FORMAT_EXAMPLE.json` for working examples!

