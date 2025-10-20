# Ticket 047: Eraser Tool - Quick Reference Guide

## 🎮 How to Use

### Activating Eraser Mode
1. **Tap the trash icon** (now always visible in edit mode)
2. Icon turns **yellow** to indicate eraser mode is active
3. **Banner appears** at bottom showing "Eraser Mode: Delete All"

### Deleting Components
- **In Delete All mode**: Tap any component or wire to delete it immediately
- **In Wires Only mode**: Tap any wire to delete it (components are safe)

### Switching Modes
- **Tap the banner** at bottom to toggle between "Delete All" and "Wires Only"
- Banner shows current mode clearly

### Deactivating Eraser Mode
- **Tap the trash icon again** to turn off eraser mode
- Icon returns to white color
- Banner disappears

---

## 🎨 Visual Indicators

### Trash Icon States
| State | Color | Meaning |
|-------|-------|---------|
| Normal | White | Eraser mode OFF |
| Active | Yellow | Eraser mode ON |

### Banner States
| Mode | Banner Text | Location |
|------|-------------|----------|
| Delete All | "Eraser Mode: Delete All" | Top of screen |
| Wires Only | "Eraser Mode: Wires Only" | Top of screen |

---

## ⚡ Quick Tips

1. **Fast Cleanup**: Activate eraser mode, then quickly tap multiple components to delete them
2. **Safe Wire Deletion**: Use "Wires Only" mode to clean up wires without accidentally deleting components
3. **Visual Feedback**: Always check the banner to confirm which mode you're in
4. **Cancel Anytime**: Tap trash icon again to exit eraser mode

---

## 🔒 Protected Elements

These elements **cannot** be deleted in eraser mode (same as normal deletion):
- Level-provided input/output pins
- Anchored pins
- Special chips disabled in level mode

---

## 📊 Workflow Comparison

### Old Workflow (2 taps)
```
Select Component → Tap Trash → Deleted
```

### New Workflow (1 tap after activation)
```
Tap Trash (activate) → Tap Component → Deleted → Tap Component → Deleted...
```

---

## 🎯 Use Cases

### Use Eraser Mode When:
- ✅ Cleaning up a large circuit quickly
- ✅ Removing multiple wires
- ✅ Deleting test components
- ✅ Quick circuit iteration

### Use Normal Mode When:
- ✅ Precise single deletions
- ✅ Deleting specific components among many
- ✅ When you want to see selection highlights first

---

## 🐛 Troubleshooting

### "Eraser mode not activating"
- Make sure you're in edit mode (can edit viewed chip)
- Check that trash icon is visible

### "Can't delete component"
- Check if it's a protected element (level pins, etc.)
- Make sure you're in "Delete All" mode (not "Wires Only")
- Verify eraser mode is active (yellow icon)

### "Banner not showing"
- Banner only appears when eraser mode is active
- Check bottom of screen
- Try toggling eraser mode off and on again

---

## 💡 Pro Tips

1. **Quick Toggle**: Keep eraser mode active while cleaning up, then toggle off when done
2. **Mode Switching**: Use banner to quickly switch between Delete All and Wires Only
3. **Safety First**: Check the banner before deleting to confirm mode
4. **Undo Available**: All deletions support undo (Ctrl+Z / Cmd+Z)

---

**Happy Circuit Building!** 🎉

