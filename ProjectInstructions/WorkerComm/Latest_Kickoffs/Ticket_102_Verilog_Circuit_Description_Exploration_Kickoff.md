# Ticket 102: Verilog / circuit description exploration

You are working on the Digital Logic Sim Mobile Unity project. This ticket is **exploratory**: we want to understand how circuits are described in **Verilog** (and optionally **BLIF** or similar formats) by looking at real files, including longer ones, and relating that to what DLS does (chips, pins, wires, hierarchy). No implementation of an importer is required—deliverable is a **report** that gives the PM and the user a clear picture of the format and how it could relate to DLS.

---

## What to do

1. **Find and open real circuit description files**
   - **Verilog**: Look at at least one or two real `.v` (or `.sv`) files. Prefer:
     - A **small** example (e.g. a single module with a few gates or a simple adder) so you can describe structure clearly.
     - A **longer** file (e.g. from an open-source CPU core, crypto block, or benchmark) so you can summarise how “a long file describing a circuit” is organised (modules, hierarchy, naming, approximate size).
   - **BLIF** (optional): If easy to find, look at a BLIF file (e.g. from Yosys output or a benchmark) and note how it describes gates and connections compared to Verilog.

2. **Understand and document**
   - **Structure**: How are inputs/outputs declared? How are gates (AND, OR, NOT, etc.) or more complex blocks described? How is hierarchy (submodules) represented?
   - **Relate to DLS**: In DLS we have chips (with input/output pins), wires between pins, and custom chips (subcircuits). Map Verilog/BLIF concepts to these: e.g. module ↔ custom chip, port ↔ pin, wire ↔ wire, instantiation ↔ placing a subchip.

3. **Write the report**
   - Save your report to **`ProjectInstructions/WorkerComm/Reports/Ticket_102_Verilog_Circuit_Description_Exploration_Report.md`**.
   - Include:
     - **Overview**: What Verilog (and BLIF if you looked at it) is and how it describes circuits in 1–2 short paragraphs.
     - **Structure**: Main constructs (modules, ports, wires, gates, hierarchy) with brief explanation and, if helpful, a small code snippet or two (paste or describe).
     - **Long file**: What you found in a longer file—how it’s organised, approximate line count, how readability/scaling works (e.g. many small modules vs one big one).
     - **Relation to DLS**: What maps directly to DLS (chips, pins, wires, custom chips); what would need adaptation or simplification for a future import (e.g. bit widths, behavioural vs structural, timing).
     - **References**: Links or paths to the example files or repos you used (e.g. GitHub RISC-V, OpenCores, or BLIF benchmarks) so the user can open them too.

---

## Where to find examples

- **Verilog**: GitHub search for “Verilog RISC-V”, “Verilog full adder”, “Verilog ALU”, or open-source repos like PicoRV32, or standard benchmarks (e.g. ISCAS, OpenCores). Pick one small and one larger file.
- **BLIF**: Yosys can compile Verilog to BLIF; BLIF benchmarks exist (e.g. from VTR or academic benchmarks). A single small BLIF file is enough to describe the format.

---

## Success criteria

- You have opened and read at least one small and one longer Verilog file (or equivalent) describing a digital circuit.
- The report clearly explains how the format describes circuits and how that relates to DLS.
- The report is saved to `ProjectInstructions/WorkerComm/Reports/Ticket_102_Verilog_Circuit_Description_Exploration_Report.md` with Status, Summary, and the sections above.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Reports/Ticket_102_Verilog_Circuit_Description_Exploration_Report.md`** with Status, Summary, and what you did (see that folder’s README for the report template). The PM will use that to update the plan and close the ticket.
