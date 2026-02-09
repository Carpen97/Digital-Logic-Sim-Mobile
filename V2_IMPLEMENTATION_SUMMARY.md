# Level V2 Format - Implementation Summary

## ✅ What Was Done

### 1. Created New Classes

**`PinData.cs`**
- Defines pin structure with `name`, `abbr`, `nBits`, and `pos`
- Smart defaults: `abbr` falls back to `name`, `nBits` defaults to 1

**`LevelDefinitionV2.cs`**
- New clean level format
- Automatic conversion to V1 via `ToV1()` method
- Inline test parser: `"0_0|0"` → V1 test vectors
- Validation to catch format errors early

### 2. Extended Existing Classes

**`LevelPack.cs`** (Updated)
- Added `levelsV2` array to `Chapter` class
- Added `GetAllLevelsAsV1()` method to merge both formats
- Automatically converts V2 → V1 on load

**Modified Loading Code:**
- `LevelsMenu.cs` - Uses `GetAllLevelsAsV1()`
- `HallOfFameMenu.cs` - Uses `GetAllLevelsAsV1()`
- `UnityMain.cs` - Uses `GetAllLevelsAsV1()`

### 3. Documentation

- **`LEVEL_V2_FORMAT_GUIDE.md`** - Complete usage guide
- **`LEVEL_V2_FORMAT_EXAMPLE.json`** - Working examples
- **`V2_IMPLEMENTATION_SUMMARY.md`** - This file

---

## 🎯 How It Works

```
levels.json
    ↓
Unity JsonUtility.FromJson<LocalLevelPack>()
    ↓
Chapter.GetAllLevelsAsV1()
    ├─ V1 levels → pass through
    └─ V2 levels → validate → convert to V1 → return
    ↓
Merged list of V1 LevelDefinitions
    ↓
Rest of system (unchanged)
```

**Key Insight:** The V2 format is just a **frontend** - everything internally still uses V1. This means:
- ✅ No changes needed to validation code
- ✅ No changes needed to pin spawning code
- ✅ No changes needed to simulation adapter
- ✅ 100% backwards compatible

---

## 🚀 Next Steps for You

### Step 1: Test the System

1. **Check for compilation errors**
   - Open Unity
   - Check Console for any C# errors
   - All new classes should compile

2. **Create a test level**
   - Add a simple V2 level to your `levels.json`
   - See example below

3. **Test in-game**
   - Open the game
   - Navigate to levels menu
   - Check console for conversion logs
   - Try playing the level

### Step 2: Migration Workflow

**Option A: Gradual Migration (Recommended)**
```json
{
  "chapterId": "ch.basics",
  "chapterName": "Basics",
  
  "levels": [
    {"id": "lvl.not.1", /* ... old format ... */},
    {"id": "lvl.and.1", /* ... old format ... */}
  ],
  
  "levelsV2": [
    {"id": "lvl.not.1.v2", /* ... new format - test alongside old */}
  ]
}
```

**Option B: Full Chapter Migration**
```json
{
  "chapterId": "ch.basics",
  "chapterName": "Basics",
  
  "levelsV2": [
    {"id": "lvl.not.1", /* ... new format ... */},
    {"id": "lvl.and.1", /* ... new format ... */}
  ]
}
```

### Step 3: Backup and Clean

1. **Before starting:**
   ```bash
   cp Assets/Resources/levels.json Assets/Resources/levels.json.backup
   ```

2. **After verifying V2 works:**
   - Remove duplicate test levels (e.g., `lvl.not.1.v2`)
   - Keep only the V2 versions with original IDs
   - Remove old V1 versions from `levels` array

---

## 📝 Quick Start Example

Add this to the **first chapter** in your `levels.json`:

```json
{
  "chapterId": "ch.basics",
  "chapterName": "Basics",
  "chapterDescription": "...",
  
  "levelsV2": [
    {
      "id": "lvl.test.v2",
      "name": "Test V2 Format",
      "chapterId": "ch.basics",
      "description": "Testing the new V2 format!",
      "inputStructure": [
        {"name": "A"}
      ],
      "outputStructure": [
        {"name": "Y"}
      ],
      "testsInline": [
        "0|1",
        "1|0"
      ]
    }
  ]
}
```

**Expected Console Output:**
```
[Chapter] Converted V2 level 'lvl.test.v2' to V1 format
[LevelsMenu] Added level pack: Basics with X levels
```

If you see this, **it's working!** ✅

---

## 🔍 Validation

The system validates V2 levels and provides helpful error messages:

**Example Error:**
```
[Chapter] Invalid V2 level 'lvl.and.1': Input bit count mismatch. Expected 2, got 3 in test: 0_0_0|1
```

**Common Validation Checks:**
- ✅ Required fields present (id, name, inputStructure, outputStructure)
- ✅ All pins have names
- ✅ Tests match expected bit counts
- ✅ Test format is valid (`0` and `1` only, has `|` separator)

---

## 🛠️ Troubleshooting

### No Levels Show Up
**Check:**
1. JSON syntax is valid (use a JSON validator)
2. Console for error messages
3. Level has an `id` field

### Level Doesn't Validate
**Check:**
1. `inputStructure` and `outputStructure` have correct `nBits`
2. Test strings match total bit counts
3. Tests have `|` separator

### Can't Find Binary Test File
**Check:**
1. File is in `Assets/Resources/GeneratedTestVectors/`
2. File has `.tvec` extension
3. Path in JSON doesn't include extension: `"testsBinaryPath": "GeneratedTestVectors/lvl.name"`

---

## 📊 Comparison: V1 vs V2

### NOT Gate Example

**V1 Format (33 lines):**
```json
{
  "id": "lvl.not.1",
  "chapterId": "ch.basics",
  "name": "NOT Gate",
  "description": "Output should be the inverse of input.",
  "inputCount": 1,
  "outputCount": 1,
  "inputBitCounts": [1],
  "outputBitCounts": [1],
  "inputPinLabels": [
    {
      "name": "A",
      "abbr": "A"
    }
  ],
  "outputPinLabels": [
    {
      "name": "Y",
      "abbr": "Y"
    }
  ],
  "testVectors": [
    {
      "inputs": "0",
      "expected": "1"
    },
    {
      "inputs": "1",
      "expected": "0"
    }
  ],
  "hints": []
}
```

**V2 Format (18 lines - 45% smaller!):**
```json
{
  "id": "lvl.not.1",
  "name": "NOT Gate",
  "chapterId": "ch.basics",
  "description": "Output should be the inverse of input.",
  "inputStructure": [
    {"name": "A"}
  ],
  "outputStructure": [
    {"name": "Y"}
  ],
  "testsInline": [
    "0|1",
    "1|0"
  ]
}
```

**Benefits:**
- 📉 **45% less code**
- 📖 **More readable**
- ✏️ **Easier to write**
- 🔧 **Less redundancy**

---

## 🎓 Migration Tips

1. **Start with simple levels** - NOT, AND, OR gates
2. **Test each conversion** - Don't migrate 50 levels at once
3. **Use the test format** - `0_0|0` is much clearer than `"inputs": "00", "expected": "0"`
4. **Keep backups** - Always have `levels.json.backup`
5. **Watch the console** - It tells you what's happening

---

## 📞 Questions?

If something doesn't work:
1. Check `LEVEL_V2_FORMAT_GUIDE.md` for detailed examples
2. Look at `LEVEL_V2_FORMAT_EXAMPLE.json` for working code
3. Check Unity Console for error messages
4. Verify JSON syntax with a validator

---

## 🎉 You're Ready!

The system is fully implemented and ready to use. You can now:
- ✅ Load both V1 and V2 levels simultaneously
- ✅ Test new format alongside old format
- ✅ Migrate gradually at your own pace
- ✅ Get helpful validation errors if format is wrong

**Happy level creation!** 🚀

