## Project Plan – Version 2.0

---

### 📌 How We Work

* **Tickets**: All work is broken down into numbered tickets (001, 002…). Only open tickets are kept here. When a ticket is completed, it is moved to [CompletedTickets.md](CompletedTickets.md) with closure details and removed from this plan.
* **Development Environment**: All development work is done in **Cursor** with integrated AI assistance. Cursor provides code completion, refactoring, debugging, and AI-powered code generation directly within the IDE.
* **Workflow**: 
  - Select a ticket from the open tickets below
  - Move it to the "In Progress" section while working on it
  - Work on implementation using Cursor's AI features
  - Test and verify the changes
  - Move to completed tickets when done
  - Use git for version control and collaboration
* **AI Integration**: Cursor's AI assistant helps with code understanding, implementation, debugging, and optimization while maintaining full context of the project.

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
| 012 | Add pin hitbox size setting            | Open   | Add a preference to adjust the hitbox size of pins for easier touch interaction.                                                                                                                                                      |
| 013 | Add scrollbar size + buttons setting   | Open   | Add a setting to adjust scrollbar size and provide optional up/down buttons to assist navigation.                                                                                                                                     |
| 018 | Share solutions (zip + ghost)          | Open   | Normalize & zip solution JSON, upload/download, and implement ghost playback viewer.                                                                                                                                                  |
| 019 | Server validation for submissions      | Open   | Cloud Function re-simulates solutions/scores to verify; flags "verified" entries and rejects impossible ones.                                                                                                                         |
| 020 | Fix layout in 'Add Special' menu       | Open   | Adjust the UI layout in the 'Add Special' menu so buttons and labels align correctly across devices.                                                                                                                                  |
| 023 | Redo customization view layout         | Open   | Redesign customization view: fix overflow of warning text (e.g., caching messages) and move display elements from left to right side.                                                                                                 |
| 024 | Component grouping system              | Open   | Add support for grouping multiple components. Selecting multiple components shows a new UI button to create a group. Groups behave as single units for selection/deselection. Groups can also be saved and spawned, similar to chips. |
| 025 | Chip preview in library menu           | Open   | Show a preview of the currently selected chip in the library menu. Use unoccupied space in the bottom-right corner for this preview.                                                                                                  |
| 026 | Perpendicular guide for straight lines | Open   | When straight line mode is toggled, draw a perpendicular guide line to assist alignment.                                                                                                                                              |
