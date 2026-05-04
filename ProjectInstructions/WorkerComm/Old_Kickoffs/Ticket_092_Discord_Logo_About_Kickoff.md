# Ticket 092: Fix Discord logo in About menu (white rectangle)

You are working on the Digital Logic Sim Mobile Unity project. Your task is to **fix the Discord logo/icon in the About menu**, which currently appears as a **white rectangle** instead of the correct Discord icon.

## What's wrong

- In the **About** menu, there is a Discord logo/icon.
- It is **not displayed correctly**: it shows as a **white rectangle** (missing or broken texture/sprite).
- The logo should display the proper Discord icon so users can recognise and tap it (e.g. to open the Discord server link).

## What to do

1. **Locate** where the About menu is drawn and where the Discord logo is set (UI code, likely a menu or drawer that shows "About" content; look for Discord, icon, sprite, or image references).
2. **Identify** why it renders as a white rectangle: missing asset reference, wrong texture/sprite assignment, wrong import settings (e.g. not readable, wrong color space), or wrong UI component setup (e.g. Image vs RawImage, missing sprite).
3. **Fix** the display so the Discord icon shows correctly (assign the correct sprite/texture, fix references, or add/replace the Discord icon asset if missing). Ensure it still fits the layout and opens the Discord link if that behaviour exists.
4. **Check** on the platform(s) you can run (Editor, and optionally a build) that the icon no longer appears as a white rectangle.

## Done when

- The Discord logo in the About menu displays the correct Discord icon (no white rectangle).
- No regressions to the rest of the About menu or to the Discord link behaviour.

When you are done (or blocked), write your report to **`ProjectInstructions/WorkerComm/Ticket_092_Discord_Logo_About_Report.md`** with Status, Summary, and what you did (see that folder's README for the report template). The PM will use that to update the plan and close the ticket.
