# Binary Test Vector Files to Create

## 📋 Summary

**Converted:** 34 levels
**File:** `levels_binary_format.json` (ready to use!)
**Binary files needed:** 34 `.tvec` files

---

## 🎯 Binary Files You Need to Create

All files should be in: `Assets/Resources/testvectors/`

### **Chapter: Basics** (6 levels)
- [ ] `lvl.not.1.tvec`
- [ ] `lvl.and.1.tvec`
- [ ] `lvl.or.1.tvec`
- [ ] `lvl.xor.1.tvec`
- [ ] `lvl.nor.1.tvec`
- [ ] `lvl.xnor.1.tvec`

### **Chapter: Intermediate** (9 levels)
- [ ] `lvl.a_and_not_b.tvec`
- [ ] `lvl.a_or_not_b.tvec`
- [ ] `lvl.impl.1.tvec`
- [ ] `lvl.and3.1.tvec`
- [ ] `lvl.or3.1.tvec`
- [ ] `lvl.maj3.1.tvec`
- [ ] `lvl.par3.1.tvec`
- [ ] `lvl.atleast1zero.tvec`
- [ ] `lvl.mux2.1.tvec`

### **Chapter: Sequential Circuits** (3 levels)
- [ ] `lvl.srlatch.1.tvec` (uses testSequences, keep original)
- [ ] `lvl.dlatch.1.tvec` (uses testSequences, keep original)
- [ ] `lvl.dflipflop.1.tvec` (uses testSequences, keep original)

**Note:** Sequential circuits use `testSequences` instead of `testVectors`, so they were not converted.

### **Chapter: Arithmetic Fundamentals** (3 levels)
- [ ] `lvl.halfadder.1.tvec`
- [ ] `lvl.fulladder.1.tvec`
- [ ] `lvl.adder4bit.1.tvec`

### **Chapter: Advanced Arithmetic** (4 levels)
- [ ] `lvl.subtractor.1.tvec`
- [ ] `lvl.comparator.1.tvec`
- [ ] `lvl.alu_simple.1.tvec`
- [ ] `lvl.alu_complete.1.tvec` ⭐ (500+ test vectors!)

### **Chapter: 8-Bit Operations** (10 levels)
- [ ] `lvl.8bit.wire.1.tvec`
- [ ] `lvl.8bit.not.1.tvec`
- [ ] `lvl.8bit.and.1.tvec`
- [ ] `lvl.8bit.or.1.tvec`
- [ ] `lvl.8bit.xor.1.tvec`
- [ ] `lvl.8bit.mux.1.tvec` ⭐ (100+ test vectors)
- [ ] `lvl.8bit.adder.1.tvec`
- [ ] `lvl.8bit.equality.1.tvec`
- [ ] `lvl.8bit.greater.1.tvec`
- [ ] `lvl.8bit.leftshift.1.tvec`
- [ ] `lvl.8bit.incrementer.1.tvec`
- [ ] `lvl.8bit.alu.1.tvec` ⭐ (200+ test vectors!)

### **Chapter: Counter** (1 level)
- [ ] `lvl.counter4bit.1.tvec` (uses testSequences, keep original)

---

## 🚀 Quick Creation Method

### **Option 1: Use the Converter Tool (Fastest)**
```
1. Tools → Digital Logic Sim → Convert Test Vectors to Binary
2. Click "Convert Large Levels to Binary"
3. Done! All .tvec files created automatically
```

### **Option 2: Generate One by One**
For each level:
```
1. Load the level in game
2. Build the reference circuit (or load saved solution)
3. Press Ctrl+V to validate
4. Click "Generate Vectors" button
5. Tools → Move Generated Vectors to Resources
```

### **Option 3: Batch Script** (If you have saved solutions)
Create a script that:
1. Loads each level
2. Loads the saved solution
3. Generates test vectors
4. Moves files automatically

---

## 📊 Expected File Sizes

| Level | Approx Size | Notes |
|-------|-------------|-------|
| Basic gates | 50-100 bytes | Very small |
| 3-input gates | 100-200 bytes | Small |
| 4-bit arithmetic | 200-500 bytes | Medium |
| 8-bit operations | 500-1000 bytes | Medium-large |
| 8-bit MUX | ~2 KB | Large (100+ vectors) |
| 8-bit ALU | ~2-3 KB | Very large (200+ vectors) |

**Total storage:** ~10-15 KB (vs ~450 KB in JSON!)

---

## ✅ After Creating Binary Files

1. **Move files:**
   ```
   All .tvec files → Assets/Resources/testvectors/
   ```

2. **Replace levels.json:**
   ```bash
   # Backup original
   cp Assets/Resources/levels.json Assets/Resources/levels.json.backup
   
   # Replace with new version
   cp levels_binary_format.json Assets/Resources/levels.json
   ```

3. **Test in Unity:**
   - Load a few levels
   - Validate them
   - Confirm they work correctly

4. **Clean up:**
   ```bash
   rm convert_to_binary_format.py
   rm levels_binary_format.json  # (after copying to levels.json)
   ```

---

## 🎯 Priority Order

If you want to convert gradually, do these first (biggest impact):

1. ⭐ **lvl.alu_complete.1** (500+ vectors → 2 KB)
2. ⭐ **lvl.8bit.alu.1** (200+ vectors → 2-3 KB)
3. ⭐ **lvl.8bit.mux.1** (100+ vectors → 2 KB)
4. All other 8-bit levels (~20 vectors each)
5. Arithmetic levels
6. Basic gates (lowest priority, already small)

---

## 📝 What Changed in levels_binary_format.json

**Before:**
```json
{
  "id": "lvl.8bit.and.1",
  "testVectors": [
    {"inputs": "0000000000000000", "expected": "00000000"},
    {"inputs": "1111111111111111", "expected": "11111111"},
    // ... 18 more entries
  ]
}
```

**After:**
```json
{
  "id": "lvl.8bit.and.1",
  "testVectorsFile": "testvectors/lvl.8bit.and.1",
  "testVectors": []
}
```

The actual test data is now in: `Assets/Resources/testvectors/lvl.8bit.and.1.tvec`

---

## 🐛 Troubleshooting

**Error: "Resource not found: testvectors/lvl.xxx"**
- The .tvec file doesn't exist yet
- Generate it using "Generate Vectors" button
- Make sure it's in Assets/Resources/testvectors/

**Level fails validation after conversion:**
- Binary file may be corrupted
- Regenerate using "Generate Vectors"
- Verify input/output bit counts match

**Can't generate vectors for a level:**
- Need a working circuit first
- Build the solution before generating
- Or use the Converter Tool on the original levels.json

---

## 📦 Files Created

1. ✅ `levels_binary_format.json` - New levels file with binary references
2. ✅ `convert_to_binary_format.py` - Conversion script (can be deleted after use)
3. ✅ `BINARY_FILES_TO_CREATE.md` - This file

---

**Ready to use!** The hard work is done - now just need to create the 34 binary files! 🎉

