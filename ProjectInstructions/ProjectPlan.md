## Project Plan – Version 2.0

---

### 📌 How We Work

* **Tickets**: All work is broken down into numbered tickets (001, 002…). Only open tickets are kept here. When a ticket is completed, a closure note is written (including the date of closure) and saved offline; it is then removed from this plan.
* **Worker Chats**: Each ticket gets its own dedicated worker chat. Moving forward, these will be handled in **Cursor**, where the worker chat is in charge of coding. The Project Manager (this chat) creates the ticket description and sends an intro paragraph to the worker chat so it knows its role and scope. The worker chat then reports back when work is done.

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

---

### 🎯 Goals

* (High-level project goals go here)

---

### 💡 Ideas / Future Features

* (Unscoped ideas to maybe turn into tickets later)

---

### 🔄 In Progress

| ID  | Name                          | Status     | Notes                                                                                                                                    |
| --- | ----------------------------- | ---------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| None currently | - | - | All tickets are either Open or Completed. |

---

### ✨ Open Tickets

| ID  | Name                                   | Status | Notes                                                                                                                                                                                                                                 |
| --- | -------------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 019 | Server validation for submissions      | Open   | Cloud Function re-simulates solutions/scores to verify; flags "verified" entries and rejects impossible ones.                                                                                                                         |
| 024 | Component grouping system              | Open   | Add support for grouping multiple components. Selecting multiple components shows a new UI button to create a group. Groups behave as single units for selection/deselection. Groups can also be saved and spawned, similar to chips. |
| 026 | Perpendicular guide for straight lines | Open   | When straight line mode is toggled, draw a perpendicular guide line to assist alignment.                                                                                                                                              |
| 039 | Show dotted wire preview on mobile wire creation| Open   | When creating new wires on mobile, display a preview of the wire path as a dotted line to show where the wire will be placed. Enhance user experience by providing visual feedback during wire creation process. Implement dotted wire rendering for wire preview state before wire is confirmed. Mobile-specific enhancement for touch-based wire creation workflow. |
| 046 | Add donation button | Open   | Add a donation button to support development of the Digital Logic Sim Mobile project. Button should be accessible from an appropriate location (e.g., About menu, main menu, or settings). Integrate with a donation platform (PayPal, Ko-fi, Buy Me a Coffee, or similar). Design should be unobtrusive but visible, matching the existing UI theme. Include optional thank you message for supporters. Consider mobile-optimized placement and ensure it opens donation link in external browser or platform-specific handler. |
| 047 | Add toggleable eraser tool | Open   | Create a special eraser tool that toggles using the existing trash icon. Make the trash icon always visible (currently only shows when component is selected). When toggled, display banner text explaining eraser mode is active. In eraser mode, touching any component on screen will delete it immediately without requiring selection first. Banner should be tappable to switch to "wires only" mode, where only wires can be deleted. Streamline deletion workflow for faster circuit editing. Mobile-optimized tool for efficient component and wire removal. |
| 055 | Disable special chips in level mode | Open   | Prevent players from using certain special chips when playing levels to maintain educational focus and prevent cheating/trivializing challenges. Disable the following chip types in level mode: (1) Memory chips: ROM, EEPROM, RAM (ChipType.Rom_256x16, EEPROM_256x16, dev_Ram_8Bit), (2) Display chips: 7-segment, RGB displays, touchscreen, dot display, LED (ChipType.SevenSegmentDisplay, DisplayRGB, DisplayRGBTouch, DisplayDot, DisplayLED), (3) Timing chips: Pulse generator, Clock (ChipType.Pulse, Clock), (4) Input chips: Key, Button, Toggle, Detector (ChipType.Key, Button, Toggle, Detector), (5) Other special chips: Buzzer, RTC, SPS, Constant (ChipType.Buzzer, RTC, SPS, Constant_8Bit). Keep enabled: NAND gates, Tri-state buffers, In/Out pins (level-provided), Merge/Split, Buses, and custom user chips. Implementation: Filter chip library when LevelManager.IsActive is true, prevent chip placement/spawning, hide disabled chips from search/library menus, show informative message if user attempts to use disabled chip. Maintain clean educational experience focused on logic gate design. Consider adding configuration in level definitions for per-level chip restrictions in future. Files: ChipLibrary.cs, LevelManager.cs, ChipInteractionController.cs, SearchPopup.cs, ChipLibraryMenu.cs, BottomBarUI.cs. |
| 056 | Make scroll views draggable by content (not just scrollbar) | Open   | Enhance scrollable content areas to support direct content dragging in addition to scrollbar interaction, providing more intuitive mobile-style scrolling. Current implementation (UI.cs DrawScrollView/DrawScrollbar) only allows scrolling via scrollbar drag or mouse wheel. Add touch/click-and-drag scrolling directly on content area for more natural mobile experience. Implementation: (1) Detect mouse/touch down events within scroll view content area (scrollArea bounds), (2) Track drag delta while mouse/touch is held down, (3) Update ScrollBarState.scrollY based on drag movement with appropriate sensitivity/momentum, (4) Add drag state to ScrollBarState to track content dragging vs scrollbar dragging, (5) Prevent interference with other UI interactions (buttons, input fields within scroll view should still work), (6) Add smooth drag-to-scroll with optional momentum/inertia for natural feel, (7) Ensure scrollbar visual updates correctly during content drag, (8) Support both vertical scrolling (current) and potentially horizontal scrolling in future. Benefits all scroll views: ROM editor, chip library, search popup, levels menu, leaderboards, patch notes, collection browser. Mobile-first enhancement that also improves PC usability. Key file: Assets/Scripts/Seb/SebVis/UI/UI.cs (DrawScrollView and DrawScrollbar methods). Consider drag threshold to distinguish between click and drag intent. |

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
- **Worker Chat Kick-off**: Prepare detailed kick-off statements in raw format (not .md) with ticket title as first line
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
- **7 Open Tickets** available for selection
- **0 In Progress** tickets
- **Project ready** for next instructions