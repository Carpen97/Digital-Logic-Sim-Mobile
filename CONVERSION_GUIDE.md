# Level Conversion Guide (V1 → V2)

## 🐍 Python Conversion Script

Use `convert_levels_to_v2.py` to automatically convert your existing V1 levels to the new V2 format.

---

## 📋 Quick Start

### 1. **Basic Conversion (All Levels)**

```bash
python convert_levels_to_v2.py
```

**What it does:**
- ✅ Reads `Assets/Resources/levels.json`
- ✅ Converts all V1 levels to V2 format
- ✅ Outputs to `levels_v2.json`
- ✅ Removes V1 levels from output (V2 only)

### 2. **Test Conversion (Keep Both Formats)**

```bash
python convert_levels_to_v2.py --keep-v1
```

**What it does:**
- ✅ Keeps V1 levels in `levels` array
- ✅ Adds V2 levels to `levelsV2` array
- ✅ Perfect for side-by-side testing!

### 3. **Convert One Chapter**

```bash
python convert_levels_to_v2.py --chapter ch.basics
```

**What it does:**
- ✅ Only converts levels in "Basics" chapter
- ✅ Other chapters remain unchanged
- ✅ Great for gradual migration

### 4. **Custom Files**

```bash
python convert_levels_to_v2.py input.json output.json
```

---

## 🎯 Usage Examples

### Example 1: Test a Single Chapter

```bash
# Convert only Basics chapter, keep V1 for comparison
python convert_levels_to_v2.py --chapter ch.basics --keep-v1 -o basics_test.json

# Copy to Unity
cp basics_test.json Assets/Resources/levels.json

# Test in Unity
# If it works, proceed to next chapter
```

### Example 2: Full Migration

```bash
# 1. Backup original
cp Assets/Resources/levels.json Assets/Resources/levels.json.backup

# 2. Convert everything
python convert_levels_to_v2.py

# 3. Review output
cat levels_v2.json | less

# 4. Replace original
cp levels_v2.json Assets/Resources/levels.json

# 5. Test in Unity
```

### Example 3: Gradual Migration

```bash
# Convert one chapter at a time, testing each
for chapter in ch.basics ch.intermediate ch.arithmetic; do
  echo "Converting $chapter..."
  python convert_levels_to_v2.py --chapter $chapter --keep-v1
  cp levels_v2.json Assets/Resources/levels.json
  echo "Test $chapter in Unity, then press Enter to continue..."
  read
done
```

---

## 🔍 What Gets Converted

### ✅ Supported (Automatic Conversion)

- **Pin structures** → `inputStructure` / `outputStructure`
- **Pin labels** → Merged with bit counts
- **Single-bit pins** → Default `nBits: 1` (omitted)
- **Multi-bit pins** → Explicit `nBits: N`
- **Test vectors** → Inline format `0_0|0`
- **Binary test files** → `testsBinaryPath`
- **All combinational circuits**

### ⚠️ Not Yet Supported (Manual)

- **Sequential circuits** (SR latch, D flip-flop, counter)
  - Script skips these with a warning
  - Need to be converted manually when sequential V2 support is added
- **Hints** (removed in V2 for now)
- **Custom pin positions** (not in V1 format)

---

## 📊 Script Output

### Successful Conversion

```
📖 Reading from: Assets/Resources/levels.json

📦 Converting chapter: Basics
  ✅ Converted: lvl.not.1
  ✅ Converted: lvl.and.1
  ✅ Converted: lvl.or.1
  ✅ Converted: lvl.xor.1
  ✅ Converted: lvl.nor.1
  ✅ Converted: lvl.xnor.1
  📊 Converted: 6, Skipped: 0

📦 Converting chapter: Sequential Circuits
  ⚠️  Skipping sequential level: lvl.srlatch.1 (not yet supported in V2)
  ⚠️  Skipping sequential level: lvl.dlatch.1 (not yet supported in V2)
  ⚠️  Skipping sequential level: lvl.dflipflop.1 (not yet supported in V2)
  📊 Converted: 0, Skipped: 3

💾 Writing to: levels_v2.json

✅ Done! Converted 48 levels to V2 format
ℹ️  V1 levels removed from output (V2 only)
```

---

## 🛠️ Conversion Logic

### Input Pin Conversion

**V1:**
```json
{
  "inputCount": 2,
  "inputBitCounts": [1, 8],
  "inputPinLabels": [
    {"name": "Select", "abbr": "S"},
    {"name": "Data", "abbr": "D"}
  ]
}
```

**V2:**
```json
{
  "inputStructure": [
    {"name": "Select", "abbr": "S"},
    {"name": "Data", "abbr": "D", "nBits": 8}
  ]
}
```

### Test Vector Conversion

**V1:**
```json
{
  "testVectors": [
    {"inputs": "000000000", "expected": "00000000"},
    {"inputs": "100000001", "expected": "00000001"}
  ]
}
```

**V2 (with 2 inputs: 1-bit + 8-bit):**
```json
{
  "testsInline": [
    "0_00000000|00000000",
    "1_00000001|00000001"
  ]
}
```

---

## 🧪 Testing Strategy

### Step 1: Convert with --keep-v1

```bash
python convert_levels_to_v2.py --keep-v1 -o levels_test.json
```

This creates a file with **both formats** side-by-side.

### Step 2: Copy to Unity

```bash
cp levels_test.json Assets/Resources/levels.json
```

### Step 3: Test in Game

- Open Unity
- Launch game
- Play a few V2 levels
- Verify they work identically to V1 versions

### Step 4: Check Console

Look for conversion messages:
```
[LevelsMenu] Converted V2 level 'lvl.not.1' to V1 format
[LevelsMenu] Converted V2 level 'lvl.and.1' to V1 format
```

### Step 5: Full Migration

Once confident, run without `--keep-v1`:
```bash
python convert_levels_to_v2.py
cp levels_v2.json Assets/Resources/levels.json
```

---

## 🔧 Troubleshooting

### Script Error: "Input file not found"

```bash
# Make sure you're in the project root
cd /path/to/Digital-Logic-Sim/

# Or specify full path
python convert_levels_to_v2.py /full/path/to/levels.json
```

### Script Error: "No chapters found"

The JSON file might be malformed. Validate it:
```bash
python -m json.tool Assets/Resources/levels.json > /dev/null
```

### Levels Don't Show Up in Unity

1. Check Unity Console for errors
2. Validate JSON: `python -m json.tool levels_v2.json`
3. Make sure you copied to the right location
4. Reload the game

### V2 Levels Fail Validation

Check Console for specific error messages:
```
[LevelsMenu] Invalid V2 level 'lvl.xyz': Input bit count mismatch...
```

This usually means the conversion missed something. Report the level ID.

---

## 📈 Advanced Usage

### Convert Specific Chapters

```bash
# Basics only
python convert_levels_to_v2.py --chapter ch.basics

# Multiple chapters (run multiple times)
for ch in ch.basics ch.intermediate; do
  python convert_levels_to_v2.py --chapter $ch --keep-v1
done
```

### Diff Before/After

```bash
# Convert
python convert_levels_to_v2.py --keep-v1

# Compare
diff Assets/Resources/levels.json levels_v2.json | less
```

### Batch Processing

```bash
#!/bin/bash
# convert_all.sh

CHAPTERS=(
  "ch.basics"
  "ch.intermediate"
  "ch.arithmetic"
  "ch.advanced_arithmetic"
  "ch.8bit"
)

for chapter in "${CHAPTERS[@]}"; do
  echo "Converting $chapter..."
  python convert_levels_to_v2.py --chapter "$chapter" --keep-v1
  cp levels_v2.json "backup_${chapter}.json"
done

echo "All chapters converted!"
```

---

## 📝 Manual Adjustments

After conversion, you might want to:

### 1. Clean Up Abbreviations

The script keeps abbreviations that match names. You can remove them:

**Before:**
```json
{"name": "A", "abbr": "A"}
```

**After:**
```json
{"name": "A"}
```

### 2. Add Underscores for Readability

For multi-bit tests, the script adds underscores automatically:

```json
"testsInline": [
  "00000000_00000001|00000010_0"
]
```

But you can add more for clarity:
```json
"testsInline": [
  "0000_0000_0000_0001|0000_0010_0"
]
```

### 3. Merge Similar Pin Names

If you have pins like `A0`, `A1`, `A2`, `A3`, consider merging to multi-bit:

**Before (V1 → V2):**
```json
"inputStructure": [
  {"name": "A0"},
  {"name": "A1"},
  {"name": "A2"},
  {"name": "A3"}
]
```

**After (manual optimization):**
```json
"inputStructure": [
  {"name": "A", "nBits": 4}
]
```

---

## ✅ Checklist

Before converting:
- [ ] Backup `levels.json`
- [ ] Python 3.7+ installed
- [ ] Script is executable (`chmod +x convert_levels_to_v2.py`)

After converting:
- [ ] Validate JSON syntax
- [ ] Check for warnings in script output
- [ ] Test in Unity
- [ ] Verify V2 levels appear in menu
- [ ] Play a few levels to confirm they work
- [ ] Check console for conversion logs

---

## 🎓 Best Practices

1. **Start small** - Convert one chapter first
2. **Use --keep-v1** - Test both formats side-by-side
3. **Backup often** - Keep multiple backups
4. **Test thoroughly** - Play converted levels
5. **Review output** - Check the JSON before using
6. **Manual polish** - Clean up abbreviations and formatting

---

**Ready to convert?** Start with:

```bash
python convert_levels_to_v2.py --chapter ch.basics --keep-v1
```

This will convert just the Basics chapter and keep both formats for testing! 🚀

