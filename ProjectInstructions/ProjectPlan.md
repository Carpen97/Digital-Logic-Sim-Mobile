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
- **`upstream/main`**: Track Seb's original updates
- **`community/main` & `community/dev`**: Track community improvements and features
- **`origin/main`**: Main mobile development branch
- **`merge/mobile-community`**: Integration branch for merging community updates into mobile
- **`mobile-port`**: Legacy mobile development branch

**Workflow:**
1. **Community Updates**: Pull from `community/dev` into `merge/mobile-community`
2. **Mobile Development**: Work on `origin/main` or create feature branches
3. **Integration**: Merge community updates into mobile branch as needed
4. **Upstream Updates**: Periodically check `upstream/main` for Seb's latest changes

---

### 🎯 Goals

* (High-level project goals go here)

---

### 💡 Ideas / Future Features

* (Unscoped ideas to maybe turn into tickets later)

---

### 🔄 In Progress

*Currently no tickets in progress*

---

### ✨ Open Tickets

| ID  | Name                                   | Status | Notes                                                                                                                                                                                                                                 |
| --- | -------------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 007 | Add iOS import/export support          | Open   | Currently project import/export works on Android. Implement equivalent functionality for iOS.                                                                                                                                         |
| 019 | Server validation for submissions      | Open   | Cloud Function re-simulates solutions/scores to verify; flags "verified" entries and rejects impossible ones.                                                                                                                         |
| 024 | Component grouping system              | Open   | Add support for grouping multiple components. Selecting multiple components shows a new UI button to create a group. Groups behave as single units for selection/deselection. Groups can also be saved and spawned, similar to chips. |
| 026 | Perpendicular guide for straight lines | Open   | When straight line mode is toggled, draw a perpendicular guide line to assist alignment.                                                                                                                                              |
| 034 | Auto-open edit tool for single component| Open   | When pressing the edit tool, if there is just one component selected that is editable, automatically open the edit tool for that component instead of requiring an additional step. Streamline the editing workflow by eliminating the need to manually select the component again when only one editable component is available. |
| 037 | Fix Firebase integration on PC            | Open   | Follow-up to Ticket 035: PC version works but Firebase integration needs fixing on PC platform. Investigate and resolve Firebase connectivity, authentication, and data synchronization issues on desktop builds. Ensure user names, score uploads, solution sharing, and leaderboard functionality work correctly on PC platform. |
| 038 | Add patch notes popup to About menu       | Open   | Create patch notes popup accessible from the About menu that displays user-facing changes and improvements since the last release. Popup should show new features, improvements, and bug fixes from a user perspective. Integrate with existing About menu system and ensure mobile-optimized display. |

---

## 📋 **Project Manager Workflow**

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
5. **📝 MANDATORY: Patch Notes Review** → **PM MUST ask: "Should this be noted in patch notes?" and update PatchNotes.md for user-facing changes**

### **Key Rules:**
- ❌ **Never move tickets** without explicit instruction
- ❌ **Never make code changes** - delegate to development teams
- ✅ **Coordinate workflow** and provide project oversight
- ✅ **Update documentation** and maintain project plans
- ✅ **Provide guidance** and technical specifications when needed
- ✅ **Always require approval** - Worker chats must discuss and get approval before making code changes
- 🔴 **MANDATORY: Git Commit Reminder** - **PM MUST proactively remind user to commit completed tickets with format: "Ticket XXX: Brief description"**
- 📝 **MANDATORY: Patch Notes Review** - **PM MUST ask "Should this be noted in patch notes?" and update PatchNotes.md for user-facing changes**

### **Current Status:**
- **7 Open Tickets** available for selection
- **0 In Progress** tickets
- **Project ready** for next instructions