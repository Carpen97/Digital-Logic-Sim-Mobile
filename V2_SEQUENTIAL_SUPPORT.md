# V2 Sequential Circuit Support

## 🎯 Overview

V2 format now supports sequential circuits with the `type` and `setup` fields!

---

## 📋 Sequential Circuit Format

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

---

## 🔑 New Fields

### `type` (string, optional)

Specifies the circuit type:
- `"combinational"` - Default, no memory (AND, OR, adders, etc.)
- `"sequential"` - Has memory/state (latches, flip-flops, counters)

**Default:** `"combinational"`

### `setup` (array of strings, optional)

Initial sequence to apply before running tests. Used to initialize sequential circuits to a known state.

**Format:** Same as `testsInline` - `"X_X|Y_Y"` or just `"X_X"`

**Example:**
```json
"setup": [
  "0_0",
  "1_0",
  "0_0"
]
```

This applies inputs `0_0`, then `1_0`, then `0_0` before running the actual tests.

---

## 🎓 Examples

### SR Latch (No Setup)

```json
{
  "id": "lvl.srlatch.1",
  "name": "SR Latch",
  "chapterId": "ch.sequential",
  "description": "Basic memory element.",
  "type": "sequential",
  "inputStructure": [
    {"name": "Reset", "abbr": "R"},
    {"name": "Set", "abbr": "S"}
  ],
  "outputStructure": [
    {"name": "Q"},
    {"name": "Q'", "abbr": "N"}
  ],
  "testsInline": [
    "0_1|1_0",  // Set: S=1 → Q=1, N=0
    "0_0|1_0",  // Hold: S=0, R=0 → Q stays 1
    "1_0|0_1",  // Reset: R=1 → Q=0, N=1
    "0_0|0_1"   // Hold: S=0, R=0 → Q stays 0
  ]
}
```

### D Latch (With Setup)

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
    {"name": "Q"},
    {"name": "Q'", "abbr": "N"}
  ],
  "setup": [
    "0_0",  // D=0, E=0
    "1_0",  // D=1, E=0
    "0_0"   // D=0, E=0
  ],
  "testsInline": [
    "0_1|1_0",  // E=1, D=0 → Q=1 (from setup)
    "0_0|1_0",  // E=0 → Q holds at 1
    "1_0|1_0",  // E=0, D changes but Q still holds
    "1_1|0_1",  // E=1, D=1 → Q=0 (transparent)
    "0_1|0_1",  // E=1, D=0 → Q=0
    "0_0|0_1"   // E=0 → Q holds at 0
  ]
}
```

### D Flip-Flop (Edge-Triggered)

```json
{
  "id": "lvl.dflipflop.1",
  "name": "D Flip-Flop",
  "chapterId": "ch.sequential",
  "description": "Captures D on rising clock edge.",
  "type": "sequential",
  "inputStructure": [
    {"name": "Data", "abbr": "D"},
    {"name": "Clock", "abbr": "CLK"}
  ],
  "outputStructure": [
    {"name": "Q"},
    {"name": "Q'", "abbr": "N"}
  ],
  "setup": [
    "0_0",
    "1_0",
    "0_0"
  ],
  "testsInline": [
    "1_0|1_0",  // CLK=0, D=1 → Q unchanged
    "1_1|1_0",  // CLK=1 (rising edge), D=1 → Q=1
    "0_1|1_0",  // CLK=1 (high), D=0 → Q unchanged (not edge)
    "1_1|0_1",  // CLK=1→1 (no edge), D=1 → Q unchanged
    "0_0|0_1",  // CLK=1→0 (falling), D=0 → Q unchanged
    "1_0|1_0"   // CLK=0→1 (rising), D=1 → Q=1
  ]
}
```

---

## 🔄 How It Works

### Conversion to V1 Format

The V2 `type` and `setup` fields map to V1's sequential circuit structure:

**V2:**
```json
{
  "type": "sequential",
  "setup": ["0_0", "1_0"],
  "testsInline": ["0_1|1_0", "1_0|0_1"]
}
```

**Converts to V1:**
```json
{
  "isSequential": true,
  "testSequences": [
    {
      "name": "Main Sequence",
      "setup": ["00", "10"],
      "vectors": [
        {"inputs": "01", "expected": "10"},
        {"inputs": "10", "expected": "01"}
      ]
    }
  ]
}
```

### Setup Behavior

1. **Setup inputs are applied first** (before any tests)
2. **Circuit state is initialized** based on setup sequence
3. **Tests run** with the initialized state
4. **Each test builds on previous state** (sequential nature)

---

## 📝 Setup Format Notes

### Input-Only Setup

You can specify just the inputs (no expected outputs):

```json
"setup": [
  "0_0",
  "1_0"
]
```

### Input-Output Setup

You can also include expected outputs (will be ignored during conversion):

```json
"setup": [
  "0_0|1_0",
  "1_0|0_1"
]
```

The converter extracts only the input part (before `|`).

### Why Use Setup?

**Without setup:**
```json
"testsInline": [
  "0_0|??",  // ❌ Unknown initial state!
  "0_1|1_0"
]
```

**With setup:**
```json
"setup": ["1_0", "0_0"],  // Initialize to known state
"testsInline": [
  "0_0|1_0",  // ✅ Known state from setup
  "0_1|1_0"
]
```

---

## ⚠️ Limitations

### Not Yet Supported

- **Binary test files for sequential** - Sequential circuits must use `testsInline`
- **Multiple test sequences** - V2 supports only one sequence (main sequence)
- **Clock edge markers** - V2 infers clock behavior from signal changes
- **Custom settle steps** - Uses default settle steps from V1

### Workarounds

**Need multiple sequences?** Use separate test levels:
```json
{
  "id": "lvl.srlatch.1.seq1",
  "name": "SR Latch - Sequence 1",
  "type": "sequential",
  "testsInline": [/* sequence 1 */]
},
{
  "id": "lvl.srlatch.1.seq2",
  "name": "SR Latch - Sequence 2",
  "type": "sequential",
  "testsInline": [/* sequence 2 */]
}
```

---

## ✅ Validation

The system validates:
- ✅ `type` is either `"combinational"` or `"sequential"`
- ✅ Setup input bits match `inputStructure` total bits
- ✅ Setup format is valid (0s and 1s only)

**Invalid:**
```json
{
  "type": "asynchronous",  // ❌ Invalid type
  "setup": ["abc"]         // ❌ Invalid format
}
```

---

## 🎯 Best Practices

1. **Always provide setup for complex sequential circuits**
   - Ensures consistent initial state
   - Makes tests deterministic

2. **Keep setup minimal**
   - Only what's needed to reach desired initial state
   - Usually 2-3 steps is enough

3. **Document expected behavior**
   - Use description to explain setup purpose
   - Comment test vectors if needed

4. **Test both set and reset**
   - Verify circuit can reach both states
   - Test hold behavior

---

## 🔍 Debugging Sequential Circuits

### Console Logs

Look for conversion messages:
```
[LevelsMenu] Converted V2 level 'lvl.srlatch.1' to V1 format
```

### Validation Errors

Common issues:
```
Setup input bit count mismatch. Expected 2, got 3 in setup: 0_0_0
```
→ Fix: Check `inputStructure` matches setup format

### Testing Strategy

1. **Start simple** - SR Latch with no setup
2. **Add setup** - D Latch with initialization
3. **Test edge detection** - D Flip-Flop with clock
4. **Verify state** - Counter with complex sequences

---

**See `LEVEL_V2_FORMAT_EXAMPLE.json` for working sequential circuit examples!**

