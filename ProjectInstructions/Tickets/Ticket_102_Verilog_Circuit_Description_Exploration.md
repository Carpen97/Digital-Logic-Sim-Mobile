# Ticket 102: Verilog / circuit description exploration

**Type:** Improvement (research / experimentation)  
**Status:** In Progress

---

## Summary

Explore **Verilog** (and optionally BLIF or similar) circuit description files to understand how real-world circuits are specified and how that relates to Digital Logic Sim. Look at real files—including longer ones from open-source projects—to build familiarity with the format, structure (modules, gates, wires, hierarchy), and to identify what maps to DLS concepts (chips, pins, wires, custom chips). No requirement to implement an importer in this ticket; the goal is **understanding and a written report** with findings, examples, and recommendations for any future import path.

---

## Goals

- Look at **real Verilog files** (and optionally BLIF or other netlist formats) that describe circuits.
- Understand the **structure**: modules, inputs/outputs, gates, wires, hierarchy (submodules).
- Relate the format to **DLS**: what corresponds to chips, pins, wires, custom chips?
- Produce a **report** (in WorkerComm/Reports) with:
  - Short overview of the format(s) and how circuits are described.
  - Example snippets or references to representative files (e.g. small gate-level, a larger file for “long file” context).
  - What is directly relatable to DLS; what would need mapping or simplification for a future import.
  - Optional: links or paths to open-source examples (e.g. small RISC-V or crypto blocks, or BLIF benchmarks) for future use.

---

## Out of scope (this ticket)

- Implementing a Verilog or BLIF **importer** in the game (future ticket if desired).
- Parsing or executing Verilog; only reading and analysing files for understanding.

---

## References

- Kickoff: `ProjectInstructions/WorkerComm/Latest_Kickoffs/Ticket_102_Verilog_Circuit_Description_Exploration_Kickoff.md`
- Report (when done): `ProjectInstructions/WorkerComm/Reports/Ticket_102_Verilog_Circuit_Description_Exploration_Report.md`
