# 📝 Patch Notes Editor - Usage Guide

## Overview

The **Patch Notes Editor** is a visual HTML tool for managing Digital Logic Sim patch notes. It provides a **single source of truth** workflow where you maintain notes in YAML format and automatically generate outputs for different purposes.

---

## 🎯 Workflow

```
┌─────────────────────────────┐
│  PatchNotes.yaml            │  ← Single Source of Truth
│  (Human-readable, metadata) │     (Edit with HTML tool)
└──────────────┬──────────────┘
               │
               │  Generate ↓
               │
        ┌──────┴──────┬─────────────────┐
        ▼             ▼                 ▼
  patchNotes.json  PatchNotes.md  Store Listings
  (In-game popup)  (Documentation)  (Copy-paste)
```

---

## 🚀 Getting Started

### Method 1: Open Locally (Recommended)

1. Navigate to `Scripts/` folder
2. Double-click `PatchNotesEditor.html`
3. Opens in your default browser
4. Click **"Load YAML"** and select `ProjectInstructions/PatchNotes.yaml`

### Method 2: Serve via HTTP (Auto-load)

If you want auto-loading to work:

```powershell
# Option A: Python
cd Scripts
python -m http.server 8000
# Open: http://localhost:8000/PatchNotesEditor.html

# Option B: Node.js
npx http-server Scripts -p 8000
# Open: http://localhost:8000/PatchNotesEditor.html
```

Auto-load will fetch `PatchNotes.yaml` automatically when served over HTTP.

---

## 🎨 Features

### ✅ Visual Editing
- **Edit inline** - All text is editable in the browser
- **Checkboxes** - Toggle `User-Facing` and `Needs Edit` flags
- **Add notes** - Internal notes for items that need attention
- **Drag-friendly UI** - Organized by version and section

### 🏷️ Metadata Management

Each patch note item has metadata:

- **`userFacing: true/false`**
  - ✅ `true` = Include in the in-game popup (users see it)
  - ❌ `false` = Internal only (developers only, not in game)
  
- **`needsEdit: true/false`**
  - ✏️ `true` = Needs rephrasing or review (highlighted in yellow)
  - ✅ `false` = Ready to publish
  
- **`notes` (optional)**
  - 💭 Add internal comments like "Too technical?" or "Maybe simplify this"

### 📊 Statistics Dashboard

Each version shows:
- **Total Items** - All patch notes for this version
- **User-Facing** - Items that will appear in-game
- **Needs Edit** - Items flagged for review
- **Internal Only** - Items excluded from game popup

### 🔄 Export Options

1. **💾 Save YAML** - Save your changes back to `PatchNotes.yaml`
2. **📦 Export JSON (Game)** - Generate `patchNotes.json` (only `userFacing: true` items)
3. **📄 Export Markdown** - Generate `PatchNotes.md` (all items, formatted)
4. **👁️ Preview All** - See both YAML and JSON outputs side-by-side

---

## 📝 Example Workflow

### Scenario: Adding a New Feature

1. **Open Editor**
   - Load `PatchNotes.yaml`

2. **Select Version**
   - Find the current version (e.g., 2.1.6.10)

3. **Add Feature**
   - Click **"➕ Add Item"** in the "✨ New Features" section
   - Type: "New undo/redo system for chip editing"
   
4. **Set Metadata**
   - ✅ **User-Facing**: `true` (users should know about it)
   - ✅ **Needs Edit**: `false` (text is ready)

5. **Save & Export**
   - Click **"💾 Save YAML"**
   - Click **"📦 Export JSON"** to update game file
   - Move JSON to `Assets/Resources/patchNotes.json`

6. **Done!** 🎉

---

## 🎨 Visual Indicators

### Item States

| Visual | Meaning |
|--------|---------|
| Normal background | Ready to publish, user-facing |
| Yellow border | ⚠️ Needs editing/review |
| Gray, faded | Internal only (not user-facing) |
| Yellow + Gray | Internal item that needs review |

### Section Colors

| Section | Color | Icon |
|---------|-------|------|
| Bug Fixes | Red | 🔧 |
| New Features | Green | ✨ |
| Improvements | Blue | ⚡ |

---

## 🔧 Common Tasks

### Mark Item as "Not User-Facing"

**Use Case:** You fixed an internal bug that users don't need to know about.

1. Find the item in the editor
2. **Uncheck** ☐ User-Facing
3. Item becomes grayed out
4. When exporting JSON, this item is excluded

### Flag Item for Review

**Use Case:** The wording is technical, needs simplification.

1. Check ☑ Needs Edit
2. Item gets yellow highlight
3. Optionally add notes: "Simplify for non-technical users"
4. Come back later and revise

### Add a New Version

1. Click **"➕ New Version"** in toolbar
2. Update version number (e.g., `2.1.6.11`)
3. Update release date
4. Add items to sections

### Reorder Items

**Currently:** Manual reordering in YAML file  
**Future:** Drag-and-drop in the editor (coming soon!)

---

## 📦 File Outputs

### 1. PatchNotes.yaml (Source)

```yaml
versions:
  - version: "2.1.6.10"
    releaseDate: "2025-10-12"
    bugFixes:
      - text: "Fixed validation bug"
        userFacing: true
        needsEdit: false
      - text: "Refactored internal code"
        userFacing: false
        needsEdit: false
```

- **Who uses it:** Developers
- **Purpose:** Single source of truth
- **Location:** `ProjectInstructions/PatchNotes.yaml`

### 2. patchNotes.json (Game)

```json
{
  "versions": [
    {
      "version": "2.1.6.10",
      "releaseDate": "2025-10-12",
      "sections": {
        "bugFixes": [
          "Fixed validation bug"
        ]
      }
    }
  ]
}
```

- **Who uses it:** The game (in-game popup)
- **Purpose:** "What's New" popup in About menu
- **Location:** `Assets/Resources/patchNotes.json`
- **Filter:** Only includes `userFacing: true` items

### 3. PatchNotes.md (Documentation)

```markdown
## **Version 2.1.6.10** - 2025-10-12

**Bug Fixes:**
- Fixed validation bug
- Refactored internal code

---
```

- **Who uses it:** GitHub, docs, team
- **Purpose:** Human-readable changelog
- **Location:** `ProjectInstructions/PatchNotes.md`
- **Content:** All items (including internal)

---

## 💡 Tips & Best Practices

### ✅ DO

- **Keep `userFacing: true`** for anything users would notice
- **Use `needsEdit`** flag liberally - review before release
- **Add notes** for context (why excluded, what needs fixing)
- **Be concise** - Users scan quickly
- **Lead with benefits** - "25x faster validation" not "Optimized code"

### ❌ DON'T

- **Don't duplicate** - One note per feature/fix
- **Don't be too technical** - "Fixed ChipValidator.cs line 42" → "Fixed validation bug"
- **Don't forget dates** - Update release date when finalizing
- **Don't delete** - Mark `userFacing: false` instead (keeps history)

---

## 🚨 Troubleshooting

### Editor Won't Load YAML

**Problem:** File picker doesn't load the file  
**Solution:** Check YAML syntax with [YAML Lint](https://www.yamllint.com/)

### Can't Auto-Load

**Problem:** "Load YAML" button required every time  
**Solution:** Serve over HTTP (see "Method 2" above)

### JSON Export Missing Items

**Problem:** Some items don't appear in JSON  
**Solution:** Check that `userFacing: true` for those items

### Formatting Looks Wrong

**Problem:** Text wrapping or line breaks  
**Solution:** Use plain text in YAML, no markdown formatting

---

## 🔮 Future Enhancements

Potential improvements:

- [ ] Drag-and-drop reordering
- [ ] Markdown preview for in-game popup
- [ ] Auto-save to local storage
- [ ] Version diffing (compare versions)
- [ ] Import from existing JSON/MD
- [ ] Templates for common patterns
- [ ] Export to store listing formats

---

## 📞 Support

**Questions?** Check the main project documentation in `ProjectInstructions/`.

**Found a bug in the editor?** The editor is a single HTML file - easy to customize!

---

**Happy editing! 🎉**

