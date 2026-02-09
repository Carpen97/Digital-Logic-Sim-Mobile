# Ticket 091: RGB LED chip

You are working on the Digital Logic Sim Mobile Unity project. Your task is to implement a **new built-in chip: an RGB LED**.

## What to build

- **Chip:** One new chip type, an RGB LED (or "RGB light").
- **Inputs:** Three 8-bit inputs:
  - **R** (red), **G** (green), **B** (blue)
  - Each in the range 0–255 (standard 8-bit per channel).
- **Behaviour:** The chip has an on-chip LED (or light). The **color of that LED** is determined by the current (R, G, B) input values. As the inputs change, the displayed color updates in real time.

## Scope

- Add the chip to the built-in chip set (chip type enum, description, creation in BuiltinChipCreator or equivalent).
- Simulator: read the three 8-bit inputs and drive the chip state (no simulation "output" beyond the visual).
- Rendering: draw the chip with an LED/light that shows the RGB color (R, G, B) from the inputs. Match the style of other display/output components where appropriate.
- Ensure the chip appears in the chip library and can be placed, wired, and used like other built-in chips. Consider chip description text for the library if that exists.

## Done when

- The RGB LED chip exists, has R/G/B 8-bit inputs, and the on-chip light shows the correct color in real time as inputs change.
- It is integrated with built-in chips, simulator, and scene rendering.
- No regressions to existing chips or simulation.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Ticket_091_RGB_LED_Chip_Report.md`** with Status, Summary, and what you did (see that folder's README for the report template). The PM will use that to update the plan and close the ticket.
