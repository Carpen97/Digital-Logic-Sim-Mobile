# Worker communication (PM ↔ agent handoff)

This folder is used for communication between the **Project Manager** (PM) chat and **worker agents**. It avoids long copy-paste of kick-off text and gives a clear place for workers to report back.

---

## Subfolders

| Folder | Contents |
|--------|----------|
| **Latest_Kickoffs/** | Kick-off files for tickets **currently in progress**. When you start a new ticket, put the kickoff here. |
| **Old_Kickoffs/** | Kick-off files (and related plans/guides) for **completed or inactive** tickets. When a ticket is closed, move its kickoff from Latest_Kickoffs to Old_Kickoffs. |
| **Reports/** | All worker report files (`Ticket_XXX_..._Report.md`). Workers write their report here. |

---

## Workflow

### 1. Kick-off (PM → worker)

- The PM writes the kick-off into **`Latest_Kickoffs/Ticket_XXX_Short_Description_Kickoff.md`** (e.g. `Latest_Kickoffs/Ticket_090_Sync_Community_Kickoff.md`). Include a **short description** in the name.
- **You (user):** Open a new agent chat and say something like:
  - *"Read `ProjectInstructions/WorkerComm/Latest_Kickoffs/Ticket_090_Sync_Community_Kickoff.md` and do the task. When you're done, write your report to `ProjectInstructions/WorkerComm/Reports/Ticket_090_Sync_Community_Report.md`."*

No need to copy-paste the kick-off text; the agent reads it from the file.

### 2. Report-back (worker → PM)

- The **worker agent** writes a report when done (or at checkpoints) to **`Reports/Ticket_XXX_Short_Description_Report.md`** (e.g. `Reports/Ticket_090_Sync_Community_Report.md`). Use the same ticket number and short description as the kick-off file.
- **You (user):** In the PM chat, say something like:
  - *"Ticket 090 is done; read the report and update the plan."*
- The PM reads the report file and updates [ProjectPlan.md](../ProjectPlan.md) / [CompletedTickets.md](../CompletedTickets.md) as needed, and reminds about git commit and patch notes.
- **When the ticket is closed:** Move the kickoff from `Latest_Kickoffs/` to `Old_Kickoffs/` so the next in-progress kickoffs are easy to find.

---

## File naming

| File | Written by | Purpose |
|------|------------|--------|
| `Ticket_XXX_Short_Description_Kickoff.md` | PM | Full kick-off text for the worker (context, steps, success criteria). Lives in Latest_Kickoffs or Old_Kickoffs. |
| `Ticket_XXX_Short_Description_Report.md` | Worker | Status and summary when done (or at milestones). Lives in Reports/. |

- **XXX** = ticket number (e.g. 091).
- **Short_Description** = a few words describing the ticket (use underscores), e.g. `RGB_LED_Chip`, `Discord_Logo_About`, `Sync_Community`.
- Use the same ticket number and short description in both kick-off and report filenames for the same ticket.

---

## Report template (for workers)

When writing the report file (e.g. `Ticket_091_RGB_LED_Chip_Report.md`) in **Reports/**, include at least:

- **Status:** In progress / Done / Blocked
- **Summary:** 1–2 sentences on what was done or what’s left
- **What I did:** (optional) Short list of changes or files touched
- **What’s left:** (optional) If not done, what remains or what’s blocked

This lets the PM (or user) quickly decide whether to mark the ticket done or follow up.

---

## Location (relative to repo root)

- Kickoffs (current): `ProjectInstructions/WorkerComm/Latest_Kickoffs/Ticket_XXX_..._Kickoff.md`
- Kickoffs (archived): `ProjectInstructions/WorkerComm/Old_Kickoffs/`
- Reports: `ProjectInstructions/WorkerComm/Reports/Ticket_XXX_..._Report.md`

These files are intended to be committed so we have a record of what was asked and what was reported.
