## Project Plan – Version 2.0

---

### 📌 How We Work

* **Tickets**: All work is broken down into numbered tickets (001, 002…). Only open tickets are kept here. When a ticket is completed, a closure note is written (including the date of closure) and saved offline; it is then removed from this plan.
* **Worker Chats**: Each ticket gets its own dedicated worker chat (in **Cursor**); the worker is in charge of coding. Handoff uses the **`ProjectInstructions/WorkerComm/`** folder: the PM writes the kick-off to **`Ticket_XXX_Short_Description_Kickoff.md`** (e.g. `Ticket_091_RGB_LED_Chip_Kickoff.md`) so the filename says what the ticket is about; you open a new agent and tell it to read that file (and to write its report to **`Ticket_XXX_Short_Description_Report.md`** when done). No need to copy-paste long text. The worker reports back by writing the report file; the PM (or you) reads it to update the plan and close the ticket. See **`ProjectInstructions/WorkerComm/README.md`** for the full workflow.

---

### 🌳 Git Workflow

**Repository Structure:**
```
Seb's Original (upstream)
    ↓
Community Edition (community) 
    ↓
Mobile Port (origin)
```

**Remotes:**
- `upstream`: https://github.com/SebLague/Digital-Logic-Sim.git (Seb's original)
- `community`: https://github.com/logic-mindful/Digital-Logic-Sim-Community-Edit.git (Community improvements)
- `origin`: https://github.com/Carpen97/Digital-Logic-Sim-Mobile.git (Mobile port)

**Branch Strategy:**
- **`upstream/main`**: Track Seb's original updates (read-only)
- **`community/main` & `community/dev`**: Track community improvements and features (read-only)
- **`main`** (local & `origin/main`): **PRIMARY development branch** - all mobile development happens here
- **`merge/mobile-community`**: Integration branch ONLY for merging community updates into mobile
- **`mobile-port`**: Legacy mobile development branch (deprecated)

**Current Setup:**
- **Active branch**: `main` (tracks `origin/main`)
- **Development**: All work happens on `main` branch
- **Backup**: Code also exists on `origin/merge/mobile-community` (from previous workflow)

**Workflow:**
1. **Mobile Development**: Work on `main` branch - this is where all tickets are developed
2. **Git Sync**: After each ticket completion, commit to `main` and push to `origin/main`
3. **Community Updates** (when needed): 
   - Pull from `community/dev` into `merge/mobile-community`
   - Review and test changes
   - Merge `merge/mobile-community` into `main` when ready
4. **Upstream Updates** (periodic): Check `upstream/main` for Seb's latest changes and integrate as needed

**🚨 CRITICAL: Unity Scene Safety Rules**
- ⚠️ **BEFORE ANY GIT BRANCH OPERATION**: Save Unity scenes (`Ctrl+S` / File → Save)
- ⚠️ **BEFORE SWITCHING BRANCHES**: Commit any Unity scene changes to git
- ⚠️ **NEVER switch branches** with unsaved Unity scene changes
- ⚠️ **PM MUST WARN USER** before any `git checkout`, `git merge`, or branch switching operation
- 💡 **Why**: Git branch switches update scene files, causing Unity to lose unsaved changes when it reloads

**🚨 CRITICAL: Community sync / merge – keep mobile Project Plan**
- When merging from Community Edition (e.g. Ticket 090), **do not overwrite** `ProjectInstructions/ProjectPlan.md` with the community version. The mobile project has its own ticket list, Backlog, and workflow (Type column, etc.); the community repo has a different plan.
- **Do not run `git stash`** before the merge if the only local changes are in ProjectInstructions—stashing removes the mobile plan from the working tree and the merge then replaces it with community’s file. Either commit the mobile plan first, or resolve conflicts by **keeping “ours”** for `ProjectInstructions/ProjectPlan.md`.
- **Instruction for agents:** When kicking off a community-sync task, tell the agent: “Preserve mobile’s `ProjectInstructions/ProjectPlan.md`; do not stash it or accept community’s version during merge.”

---

### 🎯 Goals

* (High-level project goals go here)

---

### 💡 Ideas / Future Features

* (Unscoped ideas to maybe turn into tickets later)

---

### 🔄 In Progress

* (None)

---

### ✨ Open Tickets

| ID  | Type    | Name                                   | Status | Notes                                                                                                                                                                                                                                 |
| --- | ------- | -------------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 019 | Feature | Server validation for submissions      | Open   | Cloud Function re-simulates solutions/scores to verify; flags "verified" entries and rejects impossible ones.                                                                                                                         |
| 024 | Feature | Component grouping system              | Open   | Add support for grouping multiple components. Selecting multiple components shows a new UI button to create a group. Groups behave as single units for selection/deselection. Groups can also be saved and spawned, similar to chips. |
| 026 | Improvement | Perpendicular guide for straight lines | Open   | When straight line mode is toggled, draw a perpendicular guide line to assist alignment.                                                                                                                                              |
| 039 | Improvement | Show dotted wire preview on mobile wire creation| Open   | When creating new wires on mobile, display a preview of the wire path as a dotted line to show where the wire will be placed. Enhance user experience by providing visual feedback during wire creation process. Implement dotted wire rendering for wire preview state before wire is confirmed. Mobile-specific enhancement for touch-based wire creation workflow. |
| 046 | Feature | Add donation button | Open   | Add a donation button to support development of the Digital Logic Sim Mobile project. Button should be accessible from an appropriate location (e.g., About menu, main menu, or settings). Integrate with a donation platform (PayPal, Ko-fi, Buy Me a Coffee, or similar). Design should be unobtrusive but visible, matching the existing UI theme. Include optional thank you message for supporters. Consider mobile-optimized placement and ensure it opens donation link in external browser or platform-specific handler. |
| 064 | Improvement | Replace buzzer text with graphical speaker icon | Open   | Replace the current text "BUZZER" display with a graphical speaker icon. Design a visual speaker representation that clearly indicates audio output state (on/off). Should be intuitive, visually appealing, and match the game's aesthetic. Consider animated states (speaker waves when active, static when silent). Mobile-optimized design with clear visual feedback. Improves visual consistency and makes buzzers more recognizable at a glance. |
| 069 | Bug     | Fix chip search functionality | Open   | Fix broken "Find chip" search feature in chip library. Search is currently non-functional and doesn't locate chips when users search by name. Implement proper search filtering, case-insensitive matching, and search across chip names, categories, and tags. Ensure search works for built-in chips, custom user chips, and chips in collections. Mobile-optimized search with clear results display and instant feedback. Critical usability feature for finding chips quickly in large libraries. |
| 071 | Feature | iOS/iPad build (.ipa distribution) | Open   | Create and distribute an .ipa file for iOS/iPad users. Set up proper iOS build configuration in Unity, handle code signing and provisioning profiles, and establish distribution method (TestFlight, direct enterprise distribution, or App Store). Ensure build is optimized for iPad screen sizes and touch controls. May require Apple Developer Program enrollment and proper certificate management. Critical for expanding user base to iOS platform. |

---

### 📥 Backlog

* Tickets below are not yet in the active queue. Move to **Open Tickets** when ready to prioritize; then move to **In Progress** when work starts.
* **Type:** Bug = fix broken behaviour; Feature = new functionality; Improvement = enhancement / UX polish.

| ID  | Type    | Name                                   | Status  | Notes                                                                                                                                                                                                                                 |
| --- | ------- | -------------------------------------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 074 | Bug     | Simulation speed not saving | Backlog | Custom sim speed (ticks per second / steps per clock tick) resets to 250/1000 on confirm. Fix persistence and application of user-entered values. Community report (Kritiv). |
| 075 | Bug     | Wire edit: restore segment deletion | Backlog | After new delete behaviour, edit-wire mode allows add/move vertices but deleting wire segments no longer works. Restore or reimplement wire-segment deletion in edit mode. Community report (Lamp). |
| 076 | Bug     | Level validation overflow / expected output | Backlog | Validation shows wrong expected output for overflow (e.g. 15+15). Fix expected-output computation and display for overflow cases so validation matches correct result. Community report (Lamp). |
| 077 | Bug     | About / Discord link (iOS) | Backlog | About: Discord icon not showing; link opens Discord app instead of server. "Coming soon" text should use Discord ID (e.g. carpen_swe) not username. Fix icon, URL, and copy. iOS report (QuanChanUwU). |
| 078 | Bug     | Delete-mode ghost hitbox | Backlog | Area at top where delete-mode popup appears stays non-interactive when popup is closed; chips cannot be moved there. Fix so zone does not block input when popup hidden. Community report (Lamp). |
| 079 | Bug     | Wire hitboxes when wires close together | Backlog | Wire hitboxes tricky when wires within 1 square. Improve hitbox/hit-test so intended wire is selected reliably (distance, priority, or feedback). Community report (Lamp). |
| 080 | Improvement | RTC / SPS abbreviations legend | Backlog | Add tooltips or short legend in sim/clock UI explaining RTC (Real Time Clock), SPS (Steps Per Second), SPCT, etc. for new players. Community request. |
| 081 | Improvement | Hall of Fame sort by score + leaderboard year | Backlog | Add option or view to sort Hall of Fame by score; add year to leaderboard entries. Community request (myithspa). |
| 082 | Feature | Nested custom chips | Backlog | Allow custom chips to contain other custom chips (nesting). Define max depth and performance considerations. Promised next patch (Lamp). |
| 083 | Feature | Save chip in level without finishing | Backlog | Allow saving current chip/progress in level mode even when level is not completed. Clarify UX: save design vs submit for score. Community request. |
| 084 | Feature | Copy circuit between projects | Backlog | Add way to copy a circuit from one project to another (e.g. project picker + copy into this project). Community request (QuanChanUwU). |
| 085 | Feature | Project or circuit descriptions | Backlog | Add optional description field for projects or individual circuits; show in project/circuit UI. Community request (QuanChanUwU). |
| 086 | Bug     | Import project / empty project bug | Backlog | Investigate: import project and "0 projects" flow show wrong or persistent error message. Repro from Discord (QuanChanUwU, iOS). |
| 087 | Feature | Progress sync between devices | Backlog | Research/backlog: cloud or account sync for progress (and optionally projects) across devices. Larger scope; auth/backend TBD. Community request (myithspa). |
| 089 | Bug     | Level zero-score loophole (disallowed components inside custom chips) | Backlog | Players can get score 0 on levels by using custom chips that contain disallowed components (e.g. ROM) inside them; level mode only restricts top-level chips. Investigate how score is computed and how level restrictions are enforced; fix so either (1) subchips are validated and disallowed components inside custom chips reject the solution or (2) score reflects actual NAND/component use so 0 is impossible when using such chips. Delegate codebase investigation and fix to worker agent. |
| 090 | Improvement | Sync with Community Edition (merge community into mobile) | Backlog | Pull latest changes from Community Edition (remote `community`, branch `community/dev`) into integration branch `merge/mobile-community`; resolve conflicts; review and test; merge `merge/mobile-community` into `main` when ready. Per project Git workflow: community is logic-mindful/Digital-Logic-Sim-Community-Edit; mobile is Carpen97/Digital-Logic-Sim-Mobile. Remember Unity scene safety: save all scenes before any branch/merge operations. |
---


## 📋 **Project Manager Workflow**

### **📦 Current Version Information:**
- **Latest Released Version:** 2.1.6.10 (Released: 2025-10-12)
- **Next Version:** 2.1.6.11 (In Development)
- **Update this section after each release!**

---

### **Role Definition:**
The Project Manager coordinates workflow, manages ticket status, and maintains project documentation. **Code implementation is delegated to development teams.**

### **Responsibilities:**
- **Ticket Management**: Move tickets between states only when explicitly instructed
- **Worker Chat Kick-off**: Write the kick-off to **`ProjectInstructions/WorkerComm/Ticket_XXX_Short_Description_Kickoff.md`** (e.g. `Ticket_091_RGB_LED_Chip_Kickoff.md`) so the filename includes a short description of the ticket. First line of the file = ticket title; then context, steps, success criteria. Tell the user to open a new agent and say: “Read `ProjectInstructions/WorkerComm/Ticket_XXX_Short_Description_Kickoff.md` and do the task; when done, write your report to `ProjectInstructions/WorkerComm/Ticket_XXX_Short_Description_Report.md`.” For community-sync / merge tasks (e.g. Ticket 090), include in the kick-off: “Preserve mobile’s `ProjectInstructions/ProjectPlan.md`; do not stash it or accept community’s version during merge.” When the user says the ticket reported back, read the corresponding report file and update the plan.
- **Workflow Coordination**: Provide guidance and oversight for development teams  
- **Documentation**: Maintain project plans and track progress
- **Status Updates**: Report on project status and coordinate next steps
- **🔴 CRITICAL: Git Management**: **ALWAYS remind user to commit completed tickets to git with clear, descriptive messages. This is mandatory after every ticket completion.**

### **Ticket Workflow:**
1. **Open Tickets** → Available for selection
2. **In Progress** → Moved only when PM receives explicit instruction
3. **Completed** → Moved to CompletedTickets.md with closure details
4. **🔴 MANDATORY: Git Commit** → **PM MUST remind user to commit completed tickets immediately with descriptive commit messages**
5. **📝 MANDATORY: Patch Notes Review** → **PM MUST ask: "Should this be noted in patch notes?" for user-facing changes**

### **📝 Patch Notes Workflow:**
- **Location:** `Assets/Resources/patchNotes.json` (single source of truth)
- **Version Tracking:** All new changes go into the NEXT version (currently 2.1.6.11)
- **Philosophy:** Log everything, refine before release
- **After Each Release:** 
  1. Update "Current Version Information" section above with new released version and next version number
  2. Create new version entry in patchNotes.json for next release
- **For Each Completed Ticket:**
  1. **PM MUST proactively add to patch notes** if user-facing (use common sense)
  2. Log to appropriate section (newFeatures, improvements, or bugFixes) in NEXT version
  3. **Always mark userFacing: true** for obvious user-facing changes
  4. **Log everything** - user reviews and refines before release
  5. **Better too much detail than too little** - can be scaled down later
- **Categories:**
  - `newFeatures` - New functionality users can try
  - `improvements` - Enhancements to existing features
  - `bugFixes` - Fixes to broken functionality
- **Before Release:** User reviews all entries, decides what users see and how to present it

### **Key Rules:**
- ❌ **Never move tickets** without explicit instruction
- ❌ **Never make code changes** - delegate to development teams
- ✅ **Coordinate workflow** and provide project oversight
- ✅ **Update documentation** and maintain project plans
- ✅ **Provide guidance** and technical specifications when needed
- ✅ **Always require approval** - Worker chats must discuss and get approval before making code changes
- 🔴 **MANDATORY: Git Commit Reminder** - **PM MUST proactively remind user to commit completed tickets with format: "Ticket XXX: Brief description"**
- 📝 **MANDATORY: Patch Notes Review** - **PM MUST ask "Should this be noted in patch notes?" and update PatchNotes.md for user-facing changes**
- 🚨 **CRITICAL: Unity Scene Safety** - **PM MUST ALWAYS warn user to save Unity scenes BEFORE any git branch operations (checkout, merge, switch, etc.)**

### **Current Status:**
- **8 Open Tickets** available for selection (019, 024, 026, 039, 046, 064, 069, 071)
- **0 In Progress** tickets
- **Tickets 070, 088, 091, 092** – completed (pre-release build, customization view pin layout, RGB LED chip, Discord logo in About menu)
- **16 Backlog** tickets (074–087, 089, 090); move to Open Tickets when ready to prioritize
- **Note:** After community merge, if ProjectPlan was reverted, restore Backlog and ticket list from this version. 044 (Unity security) may have come from community—add to Open if needed.