# Level V2 Format - Quick Reference Card

## 📋 Minimal Example

```json
{
  "id": "lvl.and.1",
  "name": "AND Gate",
  "chapterId": "ch.basics",
  "description": "Output is 1 only if both inputs are 1.",
  "inputStructure": [
    {"name": "A"},
    {"name": "B"}
  ],
  "outputStructure": [
    {"name": "Y"}
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

## 🔑 Required Fields

```json
{
  "id": "unique.level.id",           // ✅ Required
  "name": "Display Name",             // ✅ Required
  "chapterId": "ch.basics",          // ✅ Required
  "description": "Instructions...",   // ✅ Required
  "inputStructure": [...],           // ✅ Required (at least 1 pin)
  "outputStructure": [...],          // ✅ Required (at least 1 pin)
  "testsInline": [...]               // ✅ Required OR testsBinaryPath
}
```

## 🔧 Optional Fields

```json
{
  "type": "combinational",           // ❌ Optional - "combinational" (default) or "sequential"
  "setup": ["XXX", "XXX"]           // ❌ Optional - Setup sequence for sequential circuits
}
```

---

## 📌 Pin Structure

```json
{
  "name": "Data A",        // ✅ Required - Display name
  "abbr": "A",            // ❌ Optional - Defaults to name
  "nBits": 8,             // ❌ Optional - Defaults to 1
  "pos": [0, 5]           // ❌ Optional - Auto-positioned
}
```

---

## 🧪 Test Format

### Pattern: `INPUT|OUTPUT`

**Single-bit pins:**
```json
"testsInline": [
  "0|1",      // A=0 → Y=1
  "1|0"       // A=1 → Y=0
]
```

**Multiple pins (use `_` to separate):**
```json
"testsInline": [
  "0_0|0",    // A=0, B=0 → Y=0
  "0_1|0",    // A=0, B=1 → Y=0
  "1_0|0",    // A=1, B=0 → Y=0
  "1_1|1"     // A=1, B=1 → Y=1
]
```

**Multi-bit pins:**
```json
"inputStructure": [
  {"name": "A", "nBits": 8},
  {"name": "B", "nBits": 8}
],
"testsInline": [
  "00000000_00000000|00000000_0",
  "00000001_00000001|00000010_0"
]
```

### Binary File (for 50+ tests)
```json
"testsBinaryPath": "GeneratedTestVectors/lvl.name"
```

---

## 🎯 Common Patterns

### Simple 1-Input Gate
```json
{
  "id": "lvl.not.1",
  "name": "NOT Gate",
  "chapterId": "ch.basics",
  "description": "Invert the input.",
  "inputStructure": [{"name": "A"}],
  "outputStructure": [{"name": "Y"}],
  "testsInline": ["0|1", "1|0"]
}
```

### 2-Input Gate
```json
{
  "id": "lvl.and.1",
  "name": "AND Gate",
  "chapterId": "ch.basics",
  "description": "Output is 1 only if both inputs are 1.",
  "inputStructure": [
    {"name": "A"},
    {"name": "B"}
  ],
  "outputStructure": [{"name": "Y"}],
  "testsInline": ["0_0|0", "0_1|0", "1_0|0", "1_1|1"]
}
```

### 8-Bit Circuit
```json
{
  "id": "lvl.8bit.wire.1",
  "name": "8-Bit Wire",
  "chapterId": "ch.8bit",
  "description": "Connect input to output.",
  "inputStructure": [
    {"name": "Data", "abbr": "IN", "nBits": 8}
  ],
  "outputStructure": [
    {"name": "Output", "abbr": "OUT", "nBits": 8}
  ],
  "testsInline": [
    "00000000|00000000",
    "11111111|11111111",
    "10101010|10101010"
  ]
}
```

### Multi-Output Circuit
```json
{
  "id": "lvl.halfadder.1",
  "name": "Half Adder",
  "chapterId": "ch.arithmetic",
  "description": "Add two bits. Output sum and carry.",
  "inputStructure": [
    {"name": "A"},
    {"name": "B"}
  ],
  "outputStructure": [
    {"name": "Sum", "abbr": "S"},
    {"name": "Carry", "abbr": "C"}
  ],
  "testsInline": [
    "0_0|0_0",
    "0_1|1_0",
    "1_0|1_0",
    "1_1|0_1"
  ]
}
```

### Sequential Circuit (SR Latch)
```json
{
  "id": "lvl.srlatch.1",
  "name": "SR Latch",
  "chapterId": "ch.sequential",
  "description": "Set when S=1, Reset when R=1, Hold when both 0.",
  "type": "sequential",
  "inputStructure": [
    {"name": "Reset", "abbr": "R"},
    {"name": "Set", "abbr": "S"}
  ],
  "outputStructure": [
    {"name": "Output", "abbr": "Q"},
    {"name": "Output Inverted", "abbr": "N"}
  ],
  "testsInline": [
    "0_1|1_0",
    "0_0|1_0",
    "1_0|0_1",
    "0_0|0_1"
  ]
}
```

### Sequential Circuit with Setup
```json
{
  "id": "lvl.dlatch.1",
  "name": "D Latch",
  "chapterId": "ch.sequential",
  "description": "When Enable=1, Q follows D. When Enable=0, Q holds.",
  "type": "sequential",
  "inputStructure": [
    {"name": "Data", "abbr": "D"},
    {"name": "Enable", "abbr": "E"}
  ],
  "outputStructure": [
    {"name": "Output", "abbr": "Q"},
    {"name": "Output Inverted", "abbr": "N"}
  ],
  "setup": [
    "0_0",
    "1_0",
    "0_0"
  ],
  "testsInline": [
    "0_1|1_0",
    "0_0|1_0",
    "1_0|1_0",
    "1_1|0_1"
  ]
}
```

---

## ⚠️ Common Mistakes

### ❌ Missing Separator
```json
"testsInline": ["000"]  // Wrong! No | separator
```
✅ **Fix:** `"testsInline": ["00|0"]`

### ❌ Bit Count Mismatch
```json
"inputStructure": [{"name": "A"}],  // 1 bit
"testsInline": ["11111111|0"]       // 8 bits!
```
✅ **Fix:** `"inputStructure": [{"name": "A", "nBits": 8}]`

### ❌ Wrong Pin Count
```json
"inputStructure": [{"name": "A"}, {"name": "B"}],  // 2 pins
"testsInline": ["0_0_0|0"]                          // 3 pins!
```
✅ **Fix:** Add third pin or remove `_0` from test

### ❌ Missing Required Field
```json
{
  "id": "lvl.and.1",
  "name": "AND Gate"
  // Missing: chapterId, description, structures, tests!
}
```

---

## 🔄 V1 → V2 Conversion

### Before (V1)
```json
{
  "id": "lvl.and.1",
  "chapterId": "ch.basics",
  "name": "AND Gate",
  "description": "...",
  "inputCount": 2,
  "outputCount": 1,
  "inputBitCounts": [1, 1],
  "outputBitCounts": [1],
  "inputPinLabels": [
    {"name": "A", "abbr": "A"},
    {"name": "B", "abbr": "B"}
  ],
  "outputPinLabels": [
    {"name": "Y", "abbr": "Y"}
  ],
  "testVectors": [
    {"inputs": "00", "expected": "0"},
    {"inputs": "01", "expected": "0"},
    {"inputs": "10", "expected": "0"},
    {"inputs": "11", "expected": "1"}
  ]
}
```

### After (V2)
```json
{
  "id": "lvl.and.1",
  "name": "AND Gate",
  "chapterId": "ch.basics",
  "description": "...",
  "inputStructure": [
    {"name": "A"},
    {"name": "B"}
  ],
  "outputStructure": [
    {"name": "Y"}
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

## 📁 File Location

Add V2 levels to `levelsV2` array in `Assets/Resources/levels.json`:

```json
{
  "chapters": [
    {
      "chapterId": "ch.basics",
      "chapterName": "Basics",
      
      "levels": [/* V1 levels */],
      "levelsV2": [/* V2 levels */]
    }
  ]
}
```

---

## ✅ Checklist

Before adding a V2 level:
- [ ] All required fields present
- [ ] Each pin has a `name`
- [ ] Pin counts match test vector lengths
- [ ] Tests use `|` separator
- [ ] Tests contain only `0`, `1`, `_`, and `|`
- [ ] Multi-bit pins specify `nBits`

---

**See `LEVEL_V2_FORMAT_GUIDE.md` for detailed documentation.**

