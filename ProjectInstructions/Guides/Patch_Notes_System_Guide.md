# Patch Notes System Guide

## Overview

The Digital Logic Sim uses a streamlined patch notes system with a single source of truth and visual editing capabilities.

## 🎯 Single Source of Truth

**File:** `Assets/Resources/patchNotes.json`

- **One file** for everything
- **Game loads** this file directly
- **Editor modifies** this file
- **No duplication** or conversion needed

## 🛠️ Editor Tool

**Location:** `Scripts/PatchNotesEditor.html`

### How to Use:

1. **Open:** Double-click `Scripts/PatchNotesEditor.html`
2. **Load:** Click "📁 Load patchNotes.json" → Select `Assets/Resources/patchNotes.json`
3. **Edit:** Use visual interface to modify patch notes
4. **Save:** Click "💾 Save JSON" → Downloads file
5. **Copy:** Replace `Assets/Resources/patchNotes.json` with downloaded file

### Editor Features:

- **Visual editing** of all patch note sections
- **User-facing toggle** - Mark items as visible to users or internal only
- **Edit notes** - Add AI instructions for rephrasing content
- **Version management** - Add/delete versions easily
- **Preview** - See how content will appear

## 📋 JSON Structure

### Editor Format (what you edit):
```json
{
  "versions": [
    {
      "version": "2.1.6.10",
      "releaseDate": "2025-10-12",
      "newFeatures": [
        {
          "text": "New feature description",
          "userFacing": true,
          "editNotes": "AI instructions for improvement"
        }
      ],
      "improvements": [...],
      "bugFixes": [...]
    }
  ]
}
```

### Game Format (what game sees):
The C# code automatically converts editor format to game format:
```json
{
  "versions": [
    {
      "version": "2.1.6.10", 
      "releaseDate": "2025-10-12",
      "sections": {
        "newFeatures": ["New feature description"],
        "improvements": [...],
        "bugFixes": [...]
      }
    }
  ]
}
```

## 🔧 Technical Details

### C# Integration:
- **File:** `Assets/Scripts/Graphics/PatchNotesData.cs`
- **Auto-detection** of editor vs game format
- **Automatic conversion** from editor to game format
- **Only shows** `userFacing: true` items to users

### Key Classes:
- `PatchNotesLoader` - Loads and converts JSON
- `PatchNoteItem` - Individual patch note with metadata
- `PatchVersion` - Version with sections for game
- `PatchVersionEditor` - Version with editor format

## 📝 Workflow

### For New Releases:

1. **Open editor** → Load current patchNotes.json
2. **Add new version** → Click "➕ New Version"
3. **Fill in details:**
   - Version number (e.g., "2.1.6.11")
   - Release date (e.g., "2025-10-15")
   - Add items to sections (Bug Fixes, New Features, Improvements)
4. **Set user-facing** - Toggle which items users see
5. **Add edit notes** - Instructions for AI to improve wording
6. **Save JSON** → Copy to Assets/Resources/
7. **Test in game** - Check "What's New" popup

### For Content Updates:

1. **Open editor** → Load patchNotes.json
2. **Edit existing items:**
   - Modify text directly
   - Toggle user-facing status
   - Update edit notes
3. **Save and copy** back to game folder

## ✅ Benefits

- **Single source of truth** - No duplicate files
- **Visual editing** - No raw JSON editing needed
- **Flexible** - Mark items as internal or user-facing
- **AI-friendly** - Edit notes for content improvement
- **Version controlled** - Git tracks the JSON file
- **Backwards compatible** - Handles old and new formats

## 🚨 Important Notes

- **Always copy** the downloaded JSON back to `Assets/Resources/patchNotes.json`
- **Test in game** after making changes
- **Only `userFacing: true` items** appear in the game popup
- **Edit notes are for AI** - They don't appear in game
- **Version order** - Latest version should be first in the array

## 📁 File Locations

```
Assets/Resources/
  └── patchNotes.json          ← Single source of truth

Scripts/
  └── PatchNotesEditor.html    ← Visual editor tool

Assets/Scripts/Graphics/
  └── PatchNotesData.cs        ← C# loading logic
```

---

*This system replaces the old dual-file approach (Markdown + JSON) with a single, editor-friendly solution.*
