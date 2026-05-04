# Verilog Behavioral (`always`) Synthesis – Investigation & Solution

## Executive Summary

DLS **already supports sequential logic** via structural circuits. The gap is not in DLS—it's in our Verilog importer, which only parses structural gate primitives. We can bridge this by adding a **lightweight synthesis pass** that converts simple behavioral Verilog into structural form before import.

---

## What DLS Supports (Confirmed)

### 1. Sequential Simulation
- **Simulator.cs**: Processes chips recursively; when no subchip is "ready" (feedback), it picks one at random to break deadlocks
- **InternalState**: Chips like RAM, displays use `InternalState` and detect `isRisingEdge` on clock pins
- **Sequential levels**: `isSequential`, `clockInputIndex`, `setup`, `testSequences` – validated and working

### 2. Sequential Primitives (Implicit)
- DLS has no **placeable** D flip-flop chip, but:
- **SR Latch, D Latch, D Flip-Flop levels** exist – players **build them from NAND gates**
- These work because DLS simulates structural circuits with feedback
- A D flip-flop built from NANDs (master–slave or edge-triggered) will simulate correctly

### 3. Structural SubChips
- Verilog importer produces `GroupDescription` with `SubChipDescription` (Name = "AND", "XOR", etc.)
- `ChipLibrary.TryGetChipDescription(name)` resolves to built-in chips
- Custom chips recurse into SubChips – full hierarchy supported

---

## The Bridge: Behavioral → Structural

### Current Flow
```
Verilog (structural) → VerilogParser → VerilogImporter → GroupDescription → DLS
```

### Target Flow (with synthesis)
```
Verilog (behavioral) → BehavioralSynthesis → Verilog (structural) → VerilogParser → VerilogImporter → DLS
```

---

## Phase 1: Combinational `assign` (Easiest)

**Input:**
```verilog
assign sum = a ^ b ^ c;
assign carry = (a & b) | (b & c) | (a & c);
```

**Synthesis approach:**
- Parse `assign` statements (regex or simple recursive descent)
- Build expression tree: `^`, `&`, `|`, `~` operators
- Expand to gate netlist:
  - `a ^ b` → `xor(w1, a, b)`
  - `a & b` → `and(w2, a, b)`
  - `~a` → `not(w3, a)`
  - Ternary `sel ? a : b` → MUX (2 AND + 1 OR + NOT)

**Complexity:** Low. Single pass, no state.

---

## Phase 2: Combinational `always @(*)`

**Input:**
```verilog
always @(*)
    if (sel) out = a; else out = b;
```

Or:
```verilog
always @(a or b or sel)
    case (sel)
        2'b00: out = a;
        2'b01: out = b;
        default: out = 1'b0;
    endcase
```

**Synthesis approach:**
- `if/else` → multiplexer (MUX = (a & ~sel) | (b & sel))
- `case` → decoder + OR tree (each case value is a minterm)
- Multi-bit: expand bit-by-bit or use bus-aware synthesis

**Complexity:** Medium. Control flow → logic expressions → gates.

---

## Phase 3: Sequential `always @(posedge clk)` (The Flip-Flop Case)

**Input (jatinmandav D flip-flop):**
```verilog
always @(A or CLK)
    if (CLK) C = A;
```

**Problem:** This is actually **level-sensitive** (D latch), not edge-triggered. `if (CLK) C = A` means "when CLK=1, C follows A". True edge-triggered would need `always @(posedge clk)`.

**Synthesis approach for D latch:**
- `C = A` when `CLK=1`, else hold → **D latch** (transparent when enabled)
- D latch can be built structurally: 2×2-input NAND for the SR-latch core + 2×2-input NAND for D gating
- We already had `10_d_latch.v` (deleted) – that was structural

**Synthesis approach for D flip-flop (`always @(posedge clk)`):**
- Edge-triggered: capture D on rising edge, hold otherwise
- Structure: Two D latches in series (master–slave), clock inverted for slave
- Or: Classic 6-NAND edge-triggered DFF
- We had `11_d_flip_flop.v` (deleted) – structural

**Key:** We don't need a new DLS chip type. We emit **structural Verilog** (NANDs, NOTs) that implements the DFF. The importer then builds the GroupDescription as today.

---

## Phase 4: Counters and Registers

**Input (4-bit counter):**
```verilog
always @(posedge clk)
    if (reset) count = 0;
    else count = count + 1;
```

**Synthesis approach:**
- `count = count + 1` → 4-bit adder (count → one input, 0001 → other, sum → count)
- Need 4× D flip-flops (one per bit)
- Reset: multiplexer at D input (reset ? 0 : next_count)
- Adder: structural (4× full adders)

**Complexity:** Higher. Requires:
- Register inference (which `reg`s become FFs)
- Arithmetic synthesis (adder from full adders)
- Control logic (reset mux)

---

## Phase 5: ALU / Division (Hard)

**ALU:** `case (opcode)` with add, sub, mul, div, AND, OR, etc.
- Each op → different datapath
- Synthesis: big MUX selecting output of each operator block
- Add: full adders (we have)
- Sub: add + 2's complement (inverter + 1)
- Mul/Div: Much larger – iterative or tree structures

**Division:** `for` loop, `temp = temp - divisor`
- Sequential algorithm → FSM + datapath
- Or: Restoring/non-restoring division array

**Recommendation:** Start with Phases 1–3. ALU/Division are projects by themselves.

---

## Implementation Plan

### Step 1: `assign` Parser (1–2 days)
- Add `ParseAssignStatements()` to VerilogParser or new `AssignSynthesizer` class
- Expression grammar: `wire = expr` where expr = primary | expr op expr | ~expr
- Emit gates, add to `VerilogParseResult.Gates`

### Step 2: Combinational `always @(*)` (2–3 days)
- Parse `always @(sensitivity) begin ... end`
- Handle `if (cond) x=y; else x=z;` → MUX
- Handle `case (sel) ...` → decoder + OR

### Step 3: Sequential `always @(posedge clk)` / level-sensitive (3–5 days)
- Detect `always @(posedge clk)` or `always @(A or CLK)`
- Single `reg` assignment → infer DFF or D latch
- Emit structural equivalent: D latch = 4 NANDs, DFF = 2 latches or 6 NANDs
- Register template: we maintain a small library of structural "macros" (D latch, DFF) that we instantiate

### Step 4: Integration
- In `VerilogImporter.ImportFromVerilog()`: run synthesis pass before existing parser if behavioral constructs detected
- Or: Pre-process file to expand behavioral → structural, then pass to existing pipeline

---

## Structural Primitives We Need

| Behavioral | Structural Equivalent |
|------------|-----------------------|
| `assign x = a & b` | `and(x, a, b)` |
| `assign x = a \| b` | `or(x, a, b)` |
| `assign x = ~a` | `not(x, a)` |
| `assign x = a ^ b` | `xor(x, a, b)` |
| `assign x = sel ? a : b` | `not(nsel, sel); and(t1,a,nsel); and(t2,b,sel); or(x,t1,t2)` |
| D latch (level-sensitive) | 4× NAND (standard circuit) |
| D flip-flop (edge) | 2× D latch master–slave, or 6-NAND edge-triggered |

---

## Proof of Concept: jatinmandav Flip-Flop

The jatinmandav `d-flip-flop-module.v` is:
```verilog
always @(A or CLK)
    if (CLK) C = A;
```

This is a **D latch** (level-sensitive). Synthesis:
1. When CLK=1: C follows A (transparent)
2. When CLK=0: C holds
3. Structural D latch: standard 4-NAND circuit with D and EN inputs
4. Emit as: `wire d, en; ...` + 4× `nand(...)` 
5. Parser + Importer handle it → GroupDescription → DLS

**No new DLS infrastructure needed.** We're just converting one Verilog style into another before the existing importer.

---

## Conclusion

The bridge is **synthesis**, not DLS changes. DLS already simulates sequential circuits correctly when they're built from gates. Our job is to turn `always` blocks into those gates. Starting with `assign` and simple `always @(*)` gives immediate value; adding D latch/DFF synthesis covers the flip-flop and counter cases. The ALU and Division remain future work due to their complexity.
