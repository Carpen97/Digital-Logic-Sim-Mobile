# Verilog Examples

Examples from [jatinmandav/Verilog-HDL](https://github.com/jatinmandav/Verilog-HDL) and behavioral synthesis tests.

## Examples

| File | Description | Style |
|------|-------------|-------|
| `Full-Adder-Circuit.v` | Full adder using XOR and AND gates | Structural |
| `FourToOneMUX.v` | 4-to-1 multiplexer | Structural |
| `TwoToFourDecoder.v` | 2-to-4 decoder | Structural |
| `xor_gate_assign.v` | XOR via `assign` | Behavioral (assign synthesis) |
| `d_flip_flop_module.v` | D latch via `always @(A or CLK)` | Behavioral (always synthesis) |

## Usage

Use **Verilog menu → Import from file (.v)** and select any `.v` file.

## Supported Behavioral Constructs

- **assign** – `&`, `|`, `^`, `~`, `? :` (ternary/MUX)
- **always @(*)** – combinational: `x = expr;`, `if (sel) x=a; else x=b;`
- **always @(sensitivity)** – level-sensitive latch: `if (clk) q = d;`
