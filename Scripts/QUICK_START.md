# 🚀 Quick Start: Patch Notes Editor

## What Was Built

You now have a **visual HTML editor** for managing patch notes with a single-source-of-truth workflow!

---

## 📁 Files Created

```
ProjectInstructions/
  └── PatchNotes.yaml         ← Single source of truth (edit this!)

Scripts/
  ├── PatchNotesEditor.html   ← Visual editor (open in browser)
  ├── README_PatchNotesEditor.md  ← Full documentation
  └── QUICK_START.md          ← This file
```

---

## ⚡ Getting Started (3 Steps)

### Step 1: Open the Editor

Navigate to `Scripts/` folder and **double-click** `PatchNotesEditor.html`

### Step 2: Load Your Data

Click **"📁 Load YAML"** → Select `ProjectInstructions/PatchNotes.yaml`

### Step 3: Start Editing!

- ✏️ Edit text directly in the browser
- ☑️ Toggle **User-Facing** (show in game?) 
- 📝 Add **Edit Notes** for AI to read and improve
- ➕ Add new items with "Add Item" button
- 🗑️ Delete items you don't need

---

## 💾 When You're Done

1. **Save YAML**
   - Click **"💾 Save YAML"**
   - Overwrites `PatchNotes.yaml`

2. **Export for Game**
   - Click **"📦 Export JSON (Game)"**
   - Save as `patchNotes.json`
   - Move to `Assets/Resources/patchNotes.json`

3. **Export Docs** (optional)
   - Click **"📄 Export Markdown"**
   - Save as `PatchNotes.md`
   - Move to `ProjectInstructions/PatchNotes.md`

---

## 🎯 Key Concept: User-Facing Flag

### ☑️ User-Facing = TRUE
- Item **appears** in the in-game "What's New" popup
- Users see this when they click About → What's New

### ☐ User-Facing = FALSE
- Item **excluded** from in-game popup
- Still saved in YAML (for your records)
- Useful for internal notes like "Refactored code structure"

**Example:**
```yaml
- text: "New undo/redo system"
  userFacing: true       # ← Show to users
  editNotes: ""          # ← Ready to publish
  
- text: "Refactored ChipValidator class"
  userFacing: false      # ← Internal only
  editNotes: "Maybe mention benefits?"
```

---

## 🎨 Visual Indicators

| Look | Meaning |
|------|---------|
| **White background** | Normal item |
| **Yellow textbox** | Edit Notes field (for AI to read) |
| **Gray, faded** | Internal only (not user-facing) |

---

## 📊 Dashboard Stats

Each version shows:
- **Total Items** - All notes
- **User-Facing** - Will appear in game (green)
- **Has Edit Notes** - Items with AI instructions (orange)
- **Internal Only** - Hidden from users (gray)

---

## 🔄 Current Workflow

### Old Way (Problem):
```
Edit PatchNotes.md → Copy to patchNotes.json → Keep in sync 😰
```

### New Way (Solution):
```
Edit PatchNotes.yaml → Export JSON/MD automatically ✅
```

**No more duplication!** One file to rule them all. 👑

---

## 💡 Quick Tips

1. **Don't delete items** - Just uncheck "User-Facing" to hide them
2. **Use Edit Notes** - Add instructions for AI to improve wording
3. **Leave notes blank** - When an item is ready to publish
4. **Preview before export** - Click "👁️ Preview All" to see outputs

---

## 🎬 Try It Now!

1. Open `Scripts/PatchNotesEditor.html`
2. Load `ProjectInstructions/PatchNotes.yaml`
3. Look at Version 2.1.6.10
4. See the two items with Edit Notes?
   - "Coming Soon" Chapter (editNotes: "Maybe too meta for users?")
   - "Scrollable Sequential Test Details" (editNotes: "Too technical? Maybe simplify.")
5. These have notes because we weren't sure about wording
6. You can now:
   - Edit the text based on the notes
   - Clear the Edit Notes field when done
   - Or uncheck "User-Facing" to exclude from game

---

## 📚 More Info

See `Scripts/README_PatchNotesEditor.md` for:
- Detailed feature list
- Common tasks
- Troubleshooting
- Best practices

---

## ❓ Questions?

**Q: Do I still need the old files?**  
A: You can keep `PatchNotes.md` and `patchNotes.json` for now, but they should be **generated** from YAML going forward.

**Q: Can I edit YAML directly?**  
A: Yes! YAML is human-readable. But the HTML editor is more visual and safer.

**Q: What if I mess up?**  
A: Git to the rescue! `git checkout PatchNotes.yaml` to undo.

**Q: Can I add custom fields?**  
A: Yes! Add any fields you want to YAML. The editor won't break.

---

**Enjoy your new patch notes workflow! 🎉**

