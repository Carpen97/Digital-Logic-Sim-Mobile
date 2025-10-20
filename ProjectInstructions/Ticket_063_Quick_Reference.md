# Discord Integration - Quick Reference

## TL;DR

**What:** Discord Rich Presence shows DLS activity in Discord status  
**Platform:** PC Only (Windows/Mac/Linux)  
**Time:** 1-3 days  
**Difficulty:** Easy  
**Status:** ✅ RECOMMENDED - Proceed with implementation

---

## What Users See in Discord

```
┌──────────────────────────────────────┐
│ 🎮 Playing Digital Logic Sim         │
│    Building: 4-Bit Adder             │
│    Sandbox Mode                      │
│    ⏱️ 00:15:32 elapsed                │
│    🔷 [DLS Logo]  🔸 [Sandbox Icon]  │
└──────────────────────────────────────┘
```

---

## Implementation Checklist

### Phase 1: Setup (30 min)
- [ ] Register Discord app at https://discord.com/developers
- [ ] Get Application ID
- [ ] Upload 5 icons (logo + sandbox/level/menu/leaderboard)
- [ ] Download discord-rpc-csharp library

### Phase 2: Code Integration (4-6 hours)
- [ ] Add `DiscordRPC.dll` to `Assets/Plugins/DiscordRPC/`
- [ ] Create `DiscordRichPresenceManager.cs` (copy from guide)
- [ ] Create `DiscordActivityTracker.cs` (copy from guide)
- [ ] Update `AppSettings.cs` (add `bool EnableDiscordRichPresence`)
- [ ] Add UI toggle in `PreferencesMenu.cs`
- [ ] Replace `YOUR_DISCORD_APP_ID_HERE` with actual ID

### Phase 3: Testing (2-4 hours)
- [ ] Test: Start game → Shows "In Main Menu"
- [ ] Test: Start level → Shows level name
- [ ] Test: Build chip → Shows chip name
- [ ] Test: View solution → Shows "Viewing solution"
- [ ] Test: Disable in settings → Presence clears
- [ ] Test: Discord not running → No errors
- [ ] Test: Long chip name → Truncates properly
- [ ] Build and test on Windows (primary)

### Phase 4: Deploy
- [ ] Include in next PC build
- [ ] Update release notes
- [ ] Announce to Discord community

---

## File Locations

```
Assets/
  Plugins/
    DiscordRPC/
      DiscordRPC.dll                           # ← Library

  Scripts/
    Description/
      Types/
        AppSettings.cs                         # ← Add 1 bool field
    
    Integration/                               # ← New folder
      Discord/                                 # ← New folder
        DiscordRichPresenceManager.cs          # ← New file (300 lines)
        DiscordActivityTracker.cs              # ← New file (200 lines)
    
    Graphics/
      UI/
        Menus/
          PreferencesMenu.cs                   # ← Add UI toggle
```

---

## Game State → Discord Mapping

| Game State | Discord Shows |
|------------|---------------|
| **Level Active** | "Level: [Name]" / "Solving puzzle" |
| **Sandbox Mode** | "Building: [Chip]" / "Sandbox Mode" |
| **View Solution** | "Viewing solution" / "By: [User]" |
| **Main Menu** | "In Main Menu" / "Browsing" |
| **Level Select** | "Browsing Levels" / "Level Select" |
| **Chip Customize** | "Customizing Chip" / "Design Menu" |
| **Settings** | "In Settings" / "Configuring" |

---

## Code to Change

### 1. AppSettings.cs
```csharp
public bool EnableDiscordRichPresence;  // Add this line

// In Default():
EnableDiscordRichPresence = true,       // Add this line
```

### 2. Replace Application ID
In `DiscordRichPresenceManager.cs`:
```csharp
private const string APPLICATION_ID = "123456789012345678"; // ← Your actual ID
```

### 3. Create GameObject (or add to UnityMain)
```csharp
GameObject discordManager = new GameObject("DiscordManager");
discordManager.AddComponent<DiscordRichPresenceManager>();
discordManager.AddComponent<DiscordActivityTracker>();
DontDestroyOnLoad(discordManager);
```

---

## Assets Needed

Create/upload to Discord Developer Portal:

| Asset Key | Description | Size | Where to Get |
|-----------|-------------|------|--------------|
| `dls_logo` | DLS main logo | 512x512 | Existing DLS logo |
| `icon_sandbox` | Sandbox icon | 256x256 | Design or use logo variant |
| `icon_level` | Level icon | 256x256 | Design or use logo variant |
| `icon_menu` | Menu icon | 256x256 | Design or use logo variant |
| `icon_leaderboard` | Leaderboard icon | 256x256 | Design or use logo variant |

---

## Testing Commands

```csharp
// Force update Discord presence (for debugging)
DiscordRichPresenceManager.Instance.SetPresence(
    "Test Details", 
    "Test State", 
    "dls_logo"
);

// Clear presence
DiscordRichPresenceManager.Instance.ClearPresence();

// Check if initialized
if (DiscordRichPresenceManager.Instance != null)
    Debug.Log("Discord ready");
```

---

## Common Issues & Fixes

| Problem | Solution |
|---------|----------|
| Discord not updating | Check Discord is running, wait 15 seconds |
| "Unknown Application" | Verify Application ID is correct |
| Compilation errors | Check `DiscordRPC.dll` platform settings |
| Assets not showing | Wait 5-10 min for Discord asset propagation |
| Long chip names | Automatically truncated to 125 chars + "..." |

---

## Performance

- **CPU:** < 0.01%
- **Memory:** ~1-2 MB
- **Network:** Negligible (local IPC to Discord client)
- **Battery:** No measurable impact

**Conclusion:** Zero noticeable performance impact.

---

## What NOT to Implement

❌ **Discord Overlay** - Deprecated SDK, 5-10 days work, limited value for single-player game  
💡 **Alternative:** Discord auto-detects games and shows overlay automatically (no dev work needed)

---

## Documentation Links

**Your Project:**
- Full Investigation Report: `Ticket_063_Discord_Integration_Investigation_Report.md`
- Implementation Guide: `Ticket_063_Discord_Implementation_Guide.md`

**External:**
- Discord Developers: https://discord.com/developers
- discord-rpc-csharp: https://github.com/Lachee/discord-rpc-csharp
- Rich Presence Docs: https://discord.com/developers/docs/rich-presence/how-to

---

## Community Response

**Original Request (by @Imred_Gemu):**
> "Can we see Discord overlay in DLS and show DLS activity in Discord status?"

**Your Response:**
> ✅ **Discord Activity Status:** YES! Coming in next PC update.  
> ⚠️ **Discord Overlay:** Discord's auto-overlay may work, but custom integration is deprecated.

---

## Estimated Timeline

| Day | Task | Hours |
|-----|------|-------|
| **Day 1** | Discord app setup, asset creation/upload | 2-3 |
| **Day 1-2** | Library integration, code implementation | 4-6 |
| **Day 2-3** | UI integration, settings menu | 2-3 |
| **Day 3** | Testing on Windows | 2-4 |
| **Day 4** | Mac/Linux testing (optional) | 2-4 |
| **Day 5** | Polish, bug fixes, documentation | 2-3 |

**Total:** 3-5 days (single developer)

---

## Success Criteria

✅ Discord shows "In Main Menu" when game starts  
✅ Discord updates when entering level (shows level name)  
✅ Discord updates when building chip (shows chip name)  
✅ Discord updates when viewing solution (shows username)  
✅ User can disable in settings  
✅ No errors when Discord not running  
✅ No performance impact  
✅ Works on Windows/Mac/Linux

---

## Release Notes Template

```markdown
## Discord Integration 🎮 (PC Only)

Show off what you're building! Discord users can now display their 
Digital Logic Sim activity in their Discord status.

**What your friends will see:**
- 🎯 What level you're playing
- 🔧 What circuit you're building  
- ⏱️ How long you've been working
- 🏆 When you're viewing top solutions

**How to use:**
1. Make sure Discord desktop app is running
2. Play Digital Logic Sim
3. Your activity automatically appears in Discord!

**Privacy:**
- Enable/disable in Settings → Discord Integration
- Only shows your current activity in DLS
- No personal information shared

**Platform:** Windows, Mac, Linux (requires Discord desktop app)
```

---

## Developer Notes

**Architecture:**
- Singleton pattern for `DiscordRichPresenceManager`
- Polling-based updates (every 15 seconds)
- Conditional compilation (`#if DISCORD_SUPPORTED`)
- Graceful degradation (no errors if Discord unavailable)
- Rate-limiting respected (Discord limit: 1 update per 15 seconds)

**Maintenance:**
- Update discord-rpc-csharp every 6-12 months (check GitHub releases)
- Monitor Discord Developer Changelog for API changes (rare)
- No server-side component (all local to Discord client)

**Future Enhancements:**
- Event-driven updates (more responsive)
- Show simulation status (running/paused)
- Show level completion percentage
- Show score/parts count in levels

---

**READY TO IMPLEMENT!** 🚀

Proceed with confidence - this is a proven, well-documented integration path.

