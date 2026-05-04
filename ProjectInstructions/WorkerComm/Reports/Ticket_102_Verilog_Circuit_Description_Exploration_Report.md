# Ticket 102: Verilog / Circuit Description Exploration – Report

## Status

**Done**

## Summary

Explored real Verilog circuit description files (one small full-adder example, one large PicoRV32 RISC-V core) and the BLIF format. Documented how each format describes modules, ports, wires, gates, and hierarchy. Mapped Verilog/BLIF concepts to DLS (chips, pins, wires, custom chips) and noted what would need adaptation for a future import.

---

## Overview

**Verilog** is a hardware description language (HDL) used to describe digital circuits. It is widely used in industry and academia for specifying logic at the register-transfer level (RTL) and below. Verilog describes circuits using *modules* (reusable units), *ports* (inputs/outputs), *wires* and *regs* (signals), and primitive gates (`and`, `or`, `xor`, `not`, etc.) or behavioural constructs (`assign`, `always`, `case`). Files typically use `.v` or `.sv` extensions.

**BLIF** (Berkeley Logic Interchange Format) is a lower-level textual format for describing logic circuits. Developed at UC Berkeley, it is used by synthesis tools (e.g. Yosys) as an intermediate representation. BLIF uses `.model` for circuits, `.inputs` / `.outputs` for ports, and `.names` with truth-table rows to describe combinational logic. It supports `.subckt` for hierarchy and `.latch` for sequential logic. It is more gate-centric than Verilog and does not support behavioural descriptions like `always` blocks.

---

## Structure

### Verilog constructs

| Construct | Role | Example |
|-----------|------|---------|
| **Module** | Reusable circuit block (like a chip definition) | `module fulladder(a,b,c,sum,carry);` |
| **Ports** | Inputs and outputs of a module | `input a,b,c; output sum,carry;` |
| **Wire** | Connects combinational logic; no storage | `wire sum, carry;` or `wire tempVar1;` |
| **Reg** | Holds state (sequential logic) | `reg [31:0] reg_pc;` |
| **Assign** | Continuous assignment (combinational) | `assign sum=a^b^c;` |
| **Gate primitives** | Built-in gates | `and(tempVar1, input1, input2);` `xor(sum, input1, input2, carry_in);` |
| **Instantiation** | Placing a submodule | `picorv32 #(.ENABLE_MUL(0)) core (.clk(clk), .resetn(resetn), ...);` |
| **Parameters** | Configurable module values | `parameter ENABLE_COUNTERS = 1,` |

**Small example (full adder) – behavioural style:**
```verilog
module fulladder(a,b,c,sum,carry);
input a,b,c;
output sum,carry;
wire sum,carry;

assign sum=a^b^c;                    // sum bit
assign carry=((a&b) | (b&c) | (a&c)); // carry bit
endmodule
```

**Small example (full adder) – structural style:**
```verilog
module FullAdderCircuit(input input1, input input2, input carry_in,
                        output sum, output carry_out);
xor(sum, input1, input2, carry_in);
and(tempVar1, input1, input2);
and(tempVar2, input2, carry_in);
and(tempVar3, input1, carry_in);
or(carry_out, tempVar1, tempVar2, tempVar3);
endmodule
```

### BLIF constructs

| Construct | Role | Example |
|-----------|------|---------|
| `.model` | Circuit / module name | `.model full_adder` |
| `.inputs` / `.outputs` | Primary I/O | `.inputs A B Cin` `.outputs Sum Cout` |
| `.names` | Truth table for one output | `.names A B X` with rows like `11 1` (A=1,B=1 → X=1) |
| `.subckt` | Instance of another model | `.subckt AND A=X1 B=Y1 X=Z1` |
| `.latch` | Sequential element | `.latch D Q re type` |
| `.end` | End of model | `.end` |

**BLIF AND gate example:**
```
.model AND
.inputs A B
.outputs X
.names A B X
11 1
.end
```

**BLIF XOR gate example:**
```
.model XOR
.inputs A B
.outputs X
.names A B X
01 1
10 1
.end
```

---

## Long file: PicoRV32

**File:** `picorv32.v` from [YosysHQ/picorv32](https://github.com/YosysHQ/picorv32)  
**Approximate size:** ~3050 lines, ~85 KB  
**Purpose:** Single-file RISC-V RV32I CPU core

**Organisation and scaling:**

- **Single top-level module:** The entire CPU is one large `module picorv32` with many ports (~80+ including optional formal/debug interfaces).
- **Parameters for features:** ~20 `parameter` declarations at the top (e.g. `ENABLE_COUNTERS`, `ENABLE_MUL`, `ENABLE_DIV`) control optional features (counters, multiply, divide, IRQ, etc.).
- **Internal structure:** Uses `reg` and `wire` for internal state and signals; `always @(posedge clk)` for sequential logic; `assign` and combinational `always` for datapath.
- **Optional submodules:** Some features (e.g. register file) can be split into separate modules via preprocessor macros.
- **Wrapper instantiation:** The file ends with a wrapper module (e.g. `picorv32_wrapper`) that instantiates the core with `picorv32 #(...) picorv32_core (...)`.
- **Readability:** Heavy use of `localparam`, `wire`, and `assign`; state-machine logic in `always` blocks; formatting is consistent but dense.
- **Preprocessor:** Uses `` `ifdef ``, `` `define ``, etc. for conditional formal verification, debug, and feature selection.

**Takeaway:** A large, real-world design can live in a single Verilog file. Hierarchy is used sparingly (mainly core vs wrapper). Parameters and preprocessor directives control variants and optional behaviour. This is quite different from DLS, where hierarchy (subchips) is the main way to manage complexity.

---

## Relation to DLS

### What maps directly

| Verilog/BLIF | DLS |
|--------------|-----|
| **Module** | **Chip** (custom chip definition) |
| **Module port** (input/output) | **Pin** (PinDescription: Name, ID, BitCount, etc.) |
| **Wire** (signal connecting logic) | **Wire** (WireDescription: SourcePinAddress, TargetPinAddress) |
| **Module instantiation** | **SubChip** (SubChipDescription: Name, ID, Position, reference to chip) |
| **Port connection** (`.port(wire)`) | **PinAddress** (PinID, PinOwnerID) linking wires to pins |

DLS `ChipDescription` has:
- `SubChips` → placed subcircuits (Verilog instantiations)
- `InputPins`, `OutputPins` → module ports
- `Wires` → connections between pins (source/target addresses)

DLS `SubChipDescription` has `Name` (chip type), `ID`, `Position` – analogous to instance name and placement in Verilog.

### What would need adaptation or simplification

| Verilog/BLIF feature | DLS limitation | Notes |
|----------------------|----------------|-------|
| **Bit widths** | DLS supports 1, 4, 8, 16, 32, 64+ bits per pin | Verilog `[31:0]` and similar need width mapping. DLS `PinBitCount` is discrete tiers. |
| **Behavioural constructs** | DLS is structural | `always`, `assign`, `case`, loops cannot be imported directly. Need synthesis or equivalent gate netlist. |
| **Registers and clocks** | DLS has built-in clock/toggle, no general registers | Sequential logic (flip-flops, latches) needs DLS chips that emulate it (e.g. D flip-flop). |
| **Parameters** | DLS subchips lack parameters | Parameterised modules map to multiple DLS chips or fixed configurations. |
| **Multiple drivers** | DLS wires are single-source | Verilog allows multiple drivers; import must resolve or flag conflicts. |
| **Hierarchy depth** | DLS supports custom chip hierarchy | Deep hierarchy can be mapped; naming and library resolution must be handled. |
| **Timing** | DLS has no timing model | `timescale`, delays, setup/hold are ignored. |
| **BLIF `.names`** | DLS uses explicit gate chips | Truth tables must be expanded into AND/OR/NOT subchips. |

### Suggested import strategy (high level)

1. **Target structural Verilog only** – or pre-process with a synthesis tool (e.g. Yosys) to convert behavioural Verilog to a gate-level netlist.
2. **BLIF as alternative source** – BLIF is already gate-level; easier to map `.names` and `.subckt` to DLS primitives.
3. **Flatten or preserve hierarchy** – Each `.model` / `module` becomes a `ChipDescription`; `.subckt` / instantiation becomes `SubChipDescription`.
4. **Width handling** – Map Verilog widths to nearest DLS `PinBitCount`; document truncation/expansion choices.
5. **Built-in library** – Provide DLS equivalents for common primitives (AND, OR, XOR, NOT, NAND, NOR, flip-flops, etc.).

---

## References

### Verilog examples

| Source | File | Description |
|--------|------|-------------|
| [amankhullar/Verilog](https://github.com/amankhullar/Verilog) | `fulladder.v` | Small behavioural full adder |
| [jatinmandav/Verilog-HDL](https://github.com/jatinmandav/Verilog-HDL) | `Full-Adder-Circuit/Full-Adder-Circuit.v` | Small structural full adder with XOR/AND/OR gates |
| [YosysHQ/picorv32](https://github.com/YosysHQ/picorv32) | `picorv32.v` | ~3050-line RISC-V CPU core |

### BLIF documentation

- [Berkeley Logic Interchange Format (BLIF)](http://www.cs.columbia.edu/~cs6861/sis/blif/index.html) – Columbia SIS documentation
- [BLIF Format Manual (USC benchmark project)](https://sportlab.usc.edu/~msabrishami/benchmark-project/blif.html)

### DLS project structure (for mapping)

- `ChipDescription` – main chip definition (SubChips, InputPins, OutputPins, Wires)
- `SubChipDescription` – placed instance of a chip
- `PinDescription` – pin metadata (Name, ID, BitCount, Position, etc.)
- `WireDescription` – connection from source pin to target pin (PinAddress)
- `PinAddress` – (PinID, PinOwnerID) to identify a pin within a chip

---

## Structural Verilog Import Feasibility Exploration

*Can we take the structural full adder (gate-level) input and create the circuit in DLS?*

### Target input (structural full adder)

```verilog
module FullAdderCircuit(input input1, input input2, input carry_in,
                        output sum, output carry_out);
xor(sum, input1, input2, carry_in);
and(tempVar1, input1, input2);
and(tempVar2, input2, carry_in);
and(tempVar3, input1, carry_in);
or(carry_out, tempVar1, tempVar2, tempVar3);
endmodule
```

### What DLS would need

To create this as a playable custom chip in DLS, we must produce a `ChipDescription` that loads via `DevChipInstance.LoadFromDescriptionTest`. The library must contain definitions for **XOR**, **AND**, and **OR**.

| DLS requirement | Fulfillment |
|-----------------|-------------|
| **Gate primitives** | DLS has **NAND** built-in; NOT AND/OR/XOR. Levels teach AND/OR/XOR as custom chips built from NAND. |
| **Library lookup** | `library.TryGetChipDescription("XOR", ...)` – names must match. Custom chips "AND", "OR", "XOR" must exist in the library. |
| **Subchips** | 5 subchips: 1× XOR, 3× AND, 1× OR. Each needs: Name, ID, Position, reference to chip description. |
| **Input pins** | 3 dev input pins: input1, input2, carry_in. |
| **Output pins** | 2 dev output pins: sum, carry_out. |
| **Wires** | 8 wires connecting gates and I/O. Each wire: SourcePinAddress, TargetPinAddress, ConnectionType=ToPins. |

### Gate-primitive options

| Option | Effort | Pros | Cons |
|--------|--------|------|------|
| **A) Add AND/OR/XOR as built-in ChipTypes** | Medium | Simple, fast sim, no extra library entries. | New enum values, `ProcessBuiltinChip` cases, `ChipTypeHelper` names, built-in list. |
| **B) Ship AND/OR/XOR as custom chip definitions** | Low | No Simulator changes. Use existing NAND-based designs. | Need 3 predefined `ChipDescription` JSON/blobs added to library on import. |
| **C) Expand to NAND-only at import** | High | Only NAND used; matches “fundamentals” philosophy. | Non-trivial logic to reduce XOR/AND/OR to NAND; many more gates. |

**Recommendation:** Start with **B** – predefine AND/OR/XOR as custom chips (built from NAND in DLS) and add them to the chip library when importing. This avoids core changes and works with current architecture.

### Required work breakdown

| Component | Scope | Difficulty |
|-----------|-------|------------|
| **1. Verilog parser** | Lex and parse structural subset: `module`, `input`/`output`, `wire`, gate primitives (`and`, `or`, `xor`, `not`, `nand`, `nor`, `xnor`). | Moderate. Can use a small hand-written parser or a library (e.g. Verilog parser in .NET). |
| **2. Netlist extraction** | Build graph: gates as nodes, wires as edges. Resolve names (ports, wires) to unique identifiers. | Moderate. Name resolution is the main complexity. |
| **3. ChipDescription builder** | Create `ChipDescription` with SubChips, InputPins, OutputPins, Wires. Assign IDs (via `IDGenerator` or equivalent). | Low–medium. Clear mapping to DLS types. |
| **4. Layout / positioning** | Assign `Vector2` positions to subchips. Simple auto-layout (e.g. topological sort, place in rows) suffices. | Low. Can start with fixed spacing in a grid. |
| **5. Pin addressing** | Map each wire (source wire name → target wire name) to `PinAddress`. Need pin ID conventions per gate type (e.g. NAND: in0=0, in1=1, out=2). | Low. Document pin IDs for each primitive. |
| **6. Gate-name mapping** | Map Verilog `and`/`or`/`xor` → DLS chip names. Ensure AND/OR/XOR exist in library (option B). | Low. Small lookup table. |
| **7. Integration** | UI to load `.v` file; call parser; build description; add gate primitives to library if missing; open new chip or overwrite. | Medium. Depends on existing UI patterns. |

### Verilog gate syntax specifics

Verilog primitive form: `gate_type(output, input1, input2, ...)`. Output first.

- `xor(sum, input1, input2, carry_in)` → 3-input XOR (parity). DLS 2-input XOR can be chained: `XOR(XOR(a,b), c)`.
- `and(tempVar1, input1, input2)` → 2-input AND.
- `or(carry_out, tempVar1, tempVar2, tempVar3)` → 3-input OR. DLS 2-input OR can be chained: `OR(OR(a,b), c)`.

If DLS only has 2-input gates, multi-input gates are expanded to a chain (e.g. `OR(OR(a,b),c)`).

### Pin ID conventions (for WireDescription)

From `BuiltinChipCreator.CreateNand()`:
- Input pins: ID 0 ("IN B"), 1 ("IN A")
- Output pin: ID 2 ("OUT")

Custom AND/OR/XOR would follow similar schemes (e.g. inputs 0,1; output 2). Document these for each primitive.

### Difficulty assessment

| Aspect | Assessment |
|--------|------------|
| **Feasibility** | **Yes** – structural full adder is small and maps cleanly to DLS. |
| **Scope** | ~2–4 days for MVP: parser for structural subset + ChipDescription builder + gate-primitive definitions. |
| **Risks** | (1) Verilog syntax varies; need robust handling of whitespace, comments, port styles. (2) Multi-input gates require chaining if only 2-input chips exist. |
| **Reusability** | Once built, same pipeline can handle other structural circuits (half adder, multiplexer, etc.). Behavioural Verilog would need external synthesis (e.g. Yosys) first. |

### Minimal proof-of-concept path

1. **Hardcode** the full-adder `ChipDescription` in C# (no parser), including predefined AND/OR/XOR `ChipDescription`s.
2. Add them to `ChipLibrary` and load via `LoadFromDescriptionTest`.
3. Verify the circuit simulates correctly and appears in the editor.

This validates the data model and layout before investing in a full parser.

---

## Notes

- No implementation was undertaken; this ticket is exploratory only.
- Real files were inspected via web fetch from GitHub.
- A future ticket could implement a Verilog/BLIF importer using the mappings above.
