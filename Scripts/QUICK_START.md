# 🚀 Quick Start: Patch Notes Editor

## What Was Built

You now have a **visual HTML editor** for managing patch notes with a single-source-of-truth workflow!

---

## 📁 Files

```
Assets/Resources/
  └── patchNotes.json         ← Single source of truth (edit via HTML editor)

Scripts/
  ├── PatchNotesEditor.html   ← Visual editor (open in browser)
  ├── README_PatchNotesEditor.md  ← Full documentation
  └── QUICK_START.md          ← This file
```

---

## ⚡ Getting Started (2 Steps)

### Step 1: Open the Editor

Just **double-click** `Scripts/PatchNotesEditor.html` in your file explorer

### Step 2: Load Your Data

Click **"📁 Load patchNotes.json"** → Navigate to `Assets/Resources/patchNotes.json`

### Step 3: Start Editing!

- ✏️ Edit text directly in the browser
- ☑️ Toggle **User-Facing** (show in game?) 
- 📝 Add **Edit Notes** for AI to read and improve
- ➕ Add new items with "Add Item" button
- 🗑️ Delete items you don't need

---

## 💾 When You're Done

1. **Save JSON**
   - Click **"💾 Save YAML"** (saves as JSON)
   - Overwrites `Assets/Resources/patchNotes.json`
   - **Done!** Game will load your changes automatically

---

## 🎯 Key Concept: User-Facing Flag

### ☑️ User-Facing = TRUE
- Item **appears** in the in-game "What's New" popup
- Users see this when they click About → What's New

### ☐ User-Facing = FALSE
- Item **excluded** from in-game popup
- Still saved in YAML (for your records)
- Useful for internal notes like "Refactored code structure"

**Example (in JSON file):**
```json
{
  "text": "New undo/redo system",
  "userFacing": true,
  "editNotes": ""
}
```

The HTML editor shows this visually with checkboxes and text fields.

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

## 🔄 Workflow

```
Edit patchNotes.json (via HTML editor) → Save → Game loads it ✅
```

**One file!** No conversion, no duplication. 👑

---

## 💡 Quick Tips

1. **Don't delete items** - Just uncheck "User-Facing" to hide them
2. **Use Edit Notes** - Add instructions for AI to improve wording
3. **Leave notes blank** - When an item is ready to publish
4. **Preview before export** - Click "👁️ Preview All" to see outputs

---

## 🎬 Try It Now!

1. Open `Scripts/PatchNotesEditor.html`
2. Load `Assets/Resources/patchNotes.json`
3. Look at Version 2.1.6.10
4. Edit any text directly
5. Toggle "User-Facing" checkbox
6. Add Edit Notes for items that need review
7. Click "💾 Save YAML" (saves to JSON)
8. **Done!** Game will show your changes

---

## 📚 More Info

See `Scripts/README_PatchNotesEditor.md` for:
- Detailed feature list
- Common tasks
- Troubleshooting
- Best practices

---

## ❓ Questions?

**Q: Can I edit JSON directly?**  
A: Yes! But the HTML editor is more visual, safer, and easier to use.

**Q: What if I mess up?**  
A: Git to the rescue! `git checkout Assets/Resources/patchNotes.json` to undo.

**Q: Can I add custom fields?**  
A: Yes! Add any fields you want to YAML. The editor won't break.

---

**Enjoy your new patch notes workflow! 🎉**

