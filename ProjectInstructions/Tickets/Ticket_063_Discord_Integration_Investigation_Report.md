# Ticket 063 - Discord Integration Investigation Report
**Date:** October 17, 2025  
**Status:** Investigation Complete  
**Scope:** PC Only (Windows/Mac/Linux)  
**Recommended Action:** Proceed with Rich Presence Implementation

---

## Executive Summary

✅ **Discord Rich Presence: FEASIBLE & RECOMMENDED**  
⚠️ **Discord Overlay: POSSIBLE BUT NOT RECOMMENDED**

Discord Rich Presence is a **proven, straightforward integration** for Unity games on PC. The implementation is well-documented, has minimal performance overhead, and can be completed in **1-3 days** of development time.

Discord Overlay support is technically possible but adds significant complexity with minimal benefit for a single-player puzzle/sandbox game like Digital Logic Sim.

**Recommendation:** Implement Discord Rich Presence first. Gauge community response before considering overlay.

---

## Part 1: Discord Rich Presence Investigation

### ✅ Feasibility: HIGH

Discord Rich Presence is **officially supported** and widely used by Unity games. It's a mature feature with proven implementations.

### Available SDKs/Libraries

#### Option 1: **discord-rpc-csharp** (RECOMMENDED)
- **GitHub:** https://github.com/Lachee/discord-rpc-csharp
- **Type:** Community C# wrapper around discord-rpc
- **Pros:**
  - Pure C# implementation (easier Unity integration)
  - Well-maintained and widely used
  - Simple API
  - No complex native bindings
  - Good documentation
  - Works with IL2CPP builds
- **Cons:**
  - Community maintained (not official Discord SDK)
- **Complexity:** ⭐⭐ Easy
- **Best for:** Quick implementation, cross-platform PC support

#### Option 2: **Discord Game SDK**
- **Link:** https://discord.com/developers/docs/game-sdk/sdk-starter-guide
- **Type:** Official Discord SDK (C/C++)
- **Pros:**
  - Official Discord support
  - Full feature set (Rich Presence + Overlay + more)
  - Well documented
- **Cons:**
  - Requires native library bindings (C/C++ → C#)
  - More complex setup
  - Native .dll/.so/.dylib files needed per platform
  - IL2CPP build complexity
  - **⚠️ STATUS UPDATE:** Discord Game SDK was deprecated in 2022 - Discord now recommends using direct RPC libraries
- **Complexity:** ⭐⭐⭐⭐ Hard
- **Best for:** If you need overlay or other advanced features

#### Option 3: **Unity Asset Store Plugins**
Several Discord Rich Presence plugins exist on the Unity Asset Store (typically $5-15). These are essentially pre-packaged versions of discord-rpc-csharp with Unity-specific helpers.

**Pros:**
- Pre-integrated for Unity
- Often includes example scenes
- Support from developer

**Cons:**
- Cost (minor)
- Dependency on third-party asset
- May not be updated regularly

### What Data Can Be Displayed?

Discord Rich Presence supports the following fields:

```
┌─────────────────────────────────────┐
│  Playing Digital Logic Sim          │  ← Application Name
│  Building: 4-Bit Adder              │  ← Details
│  Sandbox Mode                       │  ← State
│  ⏱️ 00:23:45 elapsed                 │  ← Timestamp
│  🔷 [Large Icon]  🔸 [Small Icon]   │  ← Images
└─────────────────────────────────────┘
```

**Available Fields:**
- **Details:** Top line of text (e.g., "Building: 4-Bit Adder")
- **State:** Second line of text (e.g., "Sandbox Mode")
- **StartTimestamp:** Shows "elapsed" time
- **EndTimestamp:** Shows countdown (useful for timed challenges)
- **LargeImageKey:** Main icon (400x400px uploaded to Discord app)
- **LargeImageText:** Tooltip for large image
- **SmallImageKey:** Small icon overlay (top-right of large image)
- **SmallImageText:** Tooltip for small image
- **PartySize/PartyMax:** Shows "X of Y" (not applicable for DLS)
- **JoinSecret/SpectateSecret:** For multiplayer (not applicable for DLS)

### Platform Support

| Platform | Rich Presence Support |
|----------|----------------------|
| Windows  | ✅ Full Support      |
| Mac      | ✅ Full Support      |
| Linux    | ✅ Full Support      |
| Android  | ❌ Not Supported     |
| iOS      | ❌ Not Supported     |

**Note:** Mobile Discord apps don't support Rich Presence for third-party apps.

### Implementation Complexity

**Estimated Effort:** 1-3 days (1 developer)

**Breakdown:**
- Discord App Registration & Asset Upload: 1-2 hours
- SDK Integration: 2-4 hours
- Activity Tracking System: 4-8 hours
- Testing & Polish: 2-4 hours
- User Settings (Enable/Disable): 1-2 hours

**Complexity Rating:** ⭐⭐ Easy to Medium

This is a **well-trodden path**. Many Unity games have successfully implemented Discord Rich Presence.

---

## Part 2: Discord Overlay Investigation

### ⚠️ Feasibility: LOW TO MEDIUM

Discord Overlay is **technically possible** but significantly more complex than Rich Presence, with questionable value for Digital Logic Sim.

### Technical Details

#### How It Works
Discord's overlay is typically enabled automatically for games it detects. However, **custom integration** requires:

1. **Discord Game SDK** (now deprecated) - Used to control overlay visibility/behavior
2. **Rendering Layer Integration** - Overlay renders on top of game using OS-level hooks
3. **Input Handling** - Coordinate input between game and overlay

#### Current Status (2024-2025)

**⚠️ IMPORTANT:** Discord deprecated the Game SDK in 2022. The overlay feature is now **auto-detected** by Discord client for known games. Manual integration is no longer officially supported for new games.

**What this means:**
- Discord will attempt to auto-detect DLS as a game
- Overlay may "just work" if Discord recognizes the executable
- No custom integration needed (but also no control)
- Can't programmatically toggle overlay visibility

### Platform Support

| Platform | Overlay Support |
|----------|----------------|
| Windows  | ✅ Auto-detect only |
| Mac      | ⚠️ Limited      |
| Linux    | ⚠️ Limited      |
| Android  | ❌ Not Supported |
| iOS      | ❌ Not Supported |

### Implementation Complexity

**Estimated Effort:** 5-10 days (IF using deprecated SDK)

**Complexity Rating:** ⭐⭐⭐⭐⭐ Very Hard

**Why so complex:**
- Requires native library integration
- Platform-specific builds (.dll/.so/.dylib)
- Rendering layer coordination
- Input handling complexities
- No official support (deprecated)

### Value Proposition for DLS

**Overlay provides:**
- In-game Discord chat visibility
- Voice channel indicators
- Friend status

**For Digital Logic Sim:**
- Single-player game (no multiplayer coordination needed)
- Puzzle/creative focus (less need for real-time communication)
- Users can already Alt+Tab easily for desktop game

**Assessment:** Overlay would be a "nice to have" but **not worth the development effort** given DLS's single-player nature.

---

## Recommended Implementation Plan

### Phase 1: Discord Rich Presence (START HERE)

#### Step 1: Discord Application Setup (1-2 hours)

1. **Register Application**
   - Go to https://discord.com/developers/applications
   - Create new application named "Digital Logic Sim"
   - Note the **Application ID** (Client ID)

2. **Upload Rich Presence Assets**
   - Large icon: DLS logo (400x400px recommended)
   - Small icons for different states:
     - `icon_sandbox` - Sandbox mode indicator
     - `icon_level` - Level mode indicator
     - `icon_menu` - Menu/browsing indicator
     - `icon_simulation` - Simulation running indicator

3. **Configure Rich Presence**
   - No additional configuration needed in Discord portal
   - All text/state controlled from game code

#### Step 2: SDK Integration (2-4 hours)

**Recommended Library:** discord-rpc-csharp

1. **Install discord-rpc-csharp**
   - Download latest release from GitHub
   - Add DLL to Unity project (`Assets/Plugins/DiscordRPC/`)
   - Platform-specific native libraries if needed

2. **Create `DiscordRichPresenceManager` Script**
   - Singleton MonoBehaviour
   - Initialize Discord client with Application ID
   - Handle connection/disconnection
   - Provide public API for updating presence

3. **Platform Detection**
   - Only initialize on PC platforms (Windows/Mac/Linux)
   - Gracefully fail on unsupported platforms
   - No errors if Discord not running

**Example Structure:**
```csharp
// Assets/Scripts/Integration/Discord/DiscordRichPresenceManager.cs
using DiscordRPC;
using UnityEngine;
using DLS.Game;
using DLS.Game.LevelsIntegration;

namespace DLS.Integration.Discord
{
    public class DiscordRichPresenceManager : MonoBehaviour
    {
        public static DiscordRichPresenceManager Instance { get; private set; }
        
        private DiscordRpcClient client;
        private bool isInitialized = false;
        
        private const string APPLICATION_ID = "YOUR_APPLICATION_ID_HERE";
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDiscord();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeDiscord()
        {
            // Only initialize on PC platforms
            #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            try
            {
                client = new DiscordRpcClient(APPLICATION_ID);
                client.Initialize();
                isInitialized = true;
                Debug.Log("[Discord] Rich Presence initialized");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Discord] Failed to initialize: {e.Message}");
                isInitialized = false;
            }
            #endif
        }
        
        public void UpdatePresence(string details, string state, string largeImageKey = "dls_logo", string smallImageKey = null)
        {
            if (!isInitialized || client == null) return;
            
            var presence = new RichPresence()
            {
                Details = details,
                State = state,
                Assets = new Assets()
                {
                    LargeImageKey = largeImageKey,
                    LargeImageText = "Digital Logic Sim",
                    SmallImageKey = smallImageKey
                },
                Timestamps = Timestamps.Now // Shows elapsed time
            };
            
            client.SetPresence(presence);
        }
        
        void OnApplicationQuit()
        {
            if (isInitialized && client != null)
            {
                client.Dispose();
            }
        }
    }
}
```

#### Step 3: Activity Tracking System (4-8 hours)

Create a system that monitors game state and updates Discord presence accordingly.

**Game States to Track:**

Based on codebase analysis, DLS has these trackable states:

1. **Main Menu**
   - `UIDrawer.ActiveMenu == UIDrawer.MenuType.MainMenu`
   - Presence: "In Main Menu" / "Browsing"

2. **Level Mode (Active)**
   - `LevelManager.Instance.IsActive == true`
   - `LevelManager.Instance.Current.levelName` available
   - Presence: "Playing Level: [LevelName]" / "Level Mode"

3. **Sandbox Mode (Editing Chip)**
   - `Project.ActiveProject != null`
   - `LevelManager.Instance.IsActive == false`
   - `Project.ActiveDevChipName` available
   - Presence: "Building: [ChipName]" / "Sandbox Mode"

4. **Viewing Leaderboard Solution**
   - `Project.ActiveProject.isViewingLeaderboardSolution == true`
   - Presence: "Viewing Solution" / "From: [Username]"

5. **Chip Customization Menu**
   - `UIDrawer.ActiveMenu == UIDrawer.MenuType.ChipCustomization`
   - Presence: "Customizing Chip" / "Design Menu"

6. **Levels Browser**
   - `UIDrawer.ActiveMenu == UIDrawer.MenuType.Levels`
   - Presence: "Browsing Levels" / "Level Select"

**Implementation Approach:**

Create a `DiscordActivityTracker` script that:
- Polls game state every 5-15 seconds (Discord rate limit: 1 update per 15 seconds)
- Compares new state to previous state
- Only updates Discord if state changed (avoid spam)
- Sanitizes chip/level names (remove special characters if needed)

**Example Structure:**
```csharp
// Assets/Scripts/Integration/Discord/DiscordActivityTracker.cs
using UnityEngine;
using DLS.Game;
using DLS.Game.LevelsIntegration;
using DLS.Graphics;

namespace DLS.Integration.Discord
{
    public class DiscordActivityTracker : MonoBehaviour
    {
        private float updateInterval = 15f; // Discord rate limit
        private float timeSinceLastUpdate = 0f;
        
        private string lastDetails = "";
        private string lastState = "";
        
        void Update()
        {
            timeSinceLastUpdate += Time.deltaTime;
            
            if (timeSinceLastUpdate >= updateInterval)
            {
                timeSinceLastUpdate = 0f;
                UpdateDiscordActivity();
            }
        }
        
        private void UpdateDiscordActivity()
        {
            string details = "Digital Logic Sim";
            string state = "In Menu";
            string largeIcon = "dls_logo";
            string smallIcon = null;
            
            // Check level mode first (highest priority)
            if (LevelManager.Instance != null && LevelManager.Instance.IsActive)
            {
                var levelName = LevelManager.Instance.Current?.levelName ?? "Unknown Level";
                details = $"Playing: {levelName}";
                state = "Level Mode";
                smallIcon = "icon_level";
            }
            // Check sandbox mode
            else if (Project.ActiveProject != null)
            {
                var chipName = Project.ActiveProject.ActiveDevChipName ?? "Untitled";
                
                // Check if viewing leaderboard solution
                if (Project.ActiveProject.isViewingLeaderboardSolution)
                {
                    details = "Viewing Solution";
                    state = $"By: {Project.ActiveProject.leaderboardSolutionUserName}";
                    smallIcon = "icon_leaderboard";
                }
                else
                {
                    details = $"Building: {chipName}";
                    state = "Sandbox Mode";
                    smallIcon = "icon_sandbox";
                }
            }
            // Check menu state
            else if (UIDrawer.ActiveMenu == UIDrawer.MenuType.MainMenu)
            {
                details = "In Main Menu";
                state = "Browsing";
                smallIcon = "icon_menu";
            }
            else if (UIDrawer.ActiveMenu == UIDrawer.MenuType.Levels)
            {
                details = "Browsing Levels";
                state = "Level Select";
                smallIcon = "icon_menu";
            }
            else if (UIDrawer.ActiveMenu == UIDrawer.MenuType.ChipCustomization)
            {
                details = "Customizing Chip";
                state = "Design Menu";
                smallIcon = "icon_customize";
            }
            
            // Only update if changed
            if (details != lastDetails || state != lastState)
            {
                lastDetails = details;
                lastState = state;
                
                DiscordRichPresenceManager.Instance?.UpdatePresence(details, state, largeIcon, smallIcon);
            }
        }
    }
}
```

#### Step 4: Integration Points (2-3 hours)

Add `DiscordRichPresenceManager` and `DiscordActivityTracker` to the game:

1. **Create Discord Manager GameObject**
   - Add to main scene or initialize in `UnityMain.cs`
   - Attach `DiscordRichPresenceManager` script
   - Attach `DiscordActivityTracker` script
   - Mark as `DontDestroyOnLoad`

2. **Alternative: Initialize in UnityMain**
   - Add Discord initialization to `UnityMain.Start()`
   - Create managers programmatically

3. **Event-Based Updates (Optional Enhancement)**
   - Instead of polling, hook into events:
     - `LevelManager.LevelStarted`
     - `LevelManager.LevelEnded`
     - Project load/save events
   - More responsive, less overhead

#### Step 5: User Settings (1-2 hours)

Add user preference to enable/disable Discord integration:

1. **Add Setting to AppSettings**
   - Add `bool EnableDiscordRichPresence = true` to `AppSettings` class
   - Save/load with existing settings system

2. **Add UI Toggle**
   - Add toggle in Settings menu
   - "Discord Integration: Show activity in Discord"

3. **Respect Setting**
   - Check setting before initializing Discord
   - Allow runtime enable/disable (may require restart)

#### Step 6: Testing (2-4 hours)

1. **Functional Testing**
   - Start game → Should show "In Main Menu"
   - Start level → Should show level name
   - Build chip → Should show chip name
   - View solution → Should show "Viewing Solution"

2. **Edge Case Testing**
   - Discord not running → Should fail gracefully
   - User not logged into Discord → Should handle gracefully
   - Long chip/level names → Should truncate/sanitize
   - Rapid state changes → Should rate-limit properly

3. **Platform Testing**
   - Windows build
   - Mac build (if accessible)
   - Linux build (if accessible)

---

## Technical Considerations

### 1. Platform-Specific Compilation

Use conditional compilation to ensure Discord code only runs on PC:

```csharp
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
    // Discord code here
#endif
```

### 2. Discord Not Running

Handle gracefully:
- Don't show errors to user
- Log warning in Unity console (for debugging)
- Continue game normally

### 3. Rate Limiting

Discord Rich Presence has rate limits:
- **1 update per 15 seconds** (recommended)
- Exceeding limit may result in temp ban
- Cache last state and only update on change

### 4. String Sanitization

Chip/level names may contain special characters:
- Truncate very long names (Discord has character limits)
- Remove/escape special Unicode characters if needed
- Details: 128 char max
- State: 128 char max

### 5. IL2CPP Compatibility

`discord-rpc-csharp` should work with IL2CPP, but:
- Test with IL2CPP build
- May need to add platform-specific native libraries
- Ensure all assemblies are properly linked

### 6. Performance Impact

Discord Rich Presence has **minimal performance impact**:
- Updates happen async (non-blocking)
- Only network I/O is lightweight JSON to local Discord client
- No rendering overhead
- Negligible CPU usage

**Expected Performance:** < 0.01% CPU overhead

---

## Discord Application Registration Details

### Assets to Prepare

Create the following image assets before registering:

1. **Large Icon (Required)**
   - **File:** DLS logo
   - **Size:** 400x400px minimum (512x512px recommended)
   - **Format:** PNG with transparency
   - **Key name:** `dls_logo`

2. **Small Icons (Optional but Recommended)**
   - **Sandbox Mode:** `icon_sandbox` (80x80px+)
   - **Level Mode:** `icon_level` (80x80px+)
   - **Menu:** `icon_menu` (80x80px+)
   - **Simulation:** `icon_simulation` (80x80px+)
   - **Leaderboard:** `icon_leaderboard` (80x80px+)

### Registration Steps

1. Go to https://discord.com/developers/applications
2. Click "New Application"
3. Name: "Digital Logic Sim"
4. Click "Create"
5. Note the **Application ID** (you'll need this in code)
6. Navigate to "Rich Presence" → "Art Assets"
7. Upload each image with corresponding key name
8. Save changes

**Time Required:** 30 minutes - 1 hour

---

## Alternative Approaches

### Approach A: Manual Presence Updates (Recommended for DLS)
- Poll game state every 15 seconds
- Simple, reliable
- Good for single-player games
- **Recommended for Digital Logic Sim**

### Approach B: Event-Driven Updates
- Hook into game events (level start, chip load, etc.)
- More responsive
- Slightly more complex
- Better for fast-paced games

### Approach C: Hybrid Approach
- Event-driven for major state changes
- Polling as fallback
- Best responsiveness + reliability
- Slight complexity increase

**Recommendation for DLS:** Start with Approach A (polling). Can upgrade to Approach C later if desired.

---

## Potential Challenges & Solutions

### Challenge 1: Discord Not Installed
**Solution:** Graceful degradation. Log warning, don't show error to user.

### Challenge 2: User Doesn't Want Activity Shared
**Solution:** Add user setting to disable. Default: enabled (but respect privacy).

### Challenge 3: Platform Fragmentation
**Solution:** PC-only feature. Use conditional compilation. Mobile users unaffected.

### Challenge 4: Library Updates
**Solution:** Use stable version of discord-rpc-csharp. Test before updating.

### Challenge 5: Long Chip/Level Names
**Solution:** Truncate to 120 characters with "..." if needed.

### Challenge 6: Special Characters in Names
**Solution:** Rich Presence supports Unicode. Test with emoji/special chars.

---

## Integration with Existing Codebase

### Existing Systems to Hook Into

Based on codebase analysis, these systems are perfect integration points:

#### 1. **LevelManager** (`Assets/Scripts/LevelsIntegration/LevelManager.cs`)
- `bool IsActive` - Check if level mode
- `LevelDefinition Current` - Get current level info
- `event Action LevelStarted` - Hook for event-driven updates
- `event Action LevelEnded` - Hook for event-driven updates

#### 2. **Project** (`Assets/Scripts/Game/Project/Project.cs`)
- `Project ActiveProject` - Check if in sandbox mode
- `string ActiveDevChipName` - Get current chip name
- `bool isViewingLeaderboardSolution` - Detect solution viewing
- `string leaderboardSolutionUserName` - Get solution author

#### 3. **UIDrawer** (`Assets/Scripts/Graphics/UI/UIDrawer.cs`)
- `MenuType ActiveMenu` - Detect current menu state

#### 4. **Main** (`Assets/Scripts/Game/Main/Main.cs`)
- Good place to initialize Discord manager
- Existing update loop for polling approach

### Minimal Code Changes Required

The beauty of Discord Rich Presence: **No changes to existing code needed!**

- Create new Discord integration scripts in separate folder
- Read existing game state (no modifications)
- Completely isolated feature (can be disabled/removed easily)

**Integration is purely additive.**

---

## File Structure

Recommended folder structure for Discord integration:

```
Assets/
  Scripts/
    Integration/                        # New folder
      Discord/                          # New folder
        DiscordRichPresenceManager.cs   # New file
        DiscordActivityTracker.cs       # New file
        DiscordSettings.cs              # New file (optional)
  Plugins/
    DiscordRPC/                         # New folder
      DiscordRPC.dll                    # discord-rpc-csharp
      [platform-specific native libs]   # If needed
```

**Total New Files:** 3-4 C# scripts + 1-2 DLLs

---

## Success Criteria

### ✅ Investigation Phase (Complete)
- [x] Discord Rich Presence feasibility determined → **FEASIBLE**
- [x] Discord Overlay feasibility determined → **NOT RECOMMENDED**
- [x] Recommended approach documented → **discord-rpc-csharp**
- [x] Platform compatibility understood → **PC Only**
- [x] Implementation complexity assessed → **1-3 Days**

### Implementation Phase (If Proceeding)
- [ ] Discord app registered and configured
- [ ] Rich Presence assets uploaded (icons)
- [ ] discord-rpc-csharp integrated into Unity project
- [ ] `DiscordRichPresenceManager` script created and tested
- [ ] `DiscordActivityTracker` script created and tested
- [ ] Activity updates based on game state (level, sandbox, menu)
- [ ] Works on Windows (primary target)
- [ ] Graceful fallback when Discord unavailable
- [ ] User setting to enable/disable (optional but recommended)
- [ ] Zero performance impact (<0.01% overhead)

---

## Cost-Benefit Analysis

### Costs
- **Development Time:** 1-3 days (1 developer)
- **Testing Time:** 0.5-1 day
- **Maintenance:** Minimal (update library occasionally)
- **Performance:** Negligible (<0.01% CPU)

### Benefits
- **Community Engagement:** Discord users see DLS activity
- **Social Discovery:** Friends see what you're playing
- **Community Request:** Directly addresses @Imred_Gemu's request
- **Marketing:** Free visibility when users play (friends see game)
- **Professionalism:** Shows polish and attention to community
- **Low Risk:** Can be disabled/removed easily

### ROI Assessment

**Value:** Medium-High  
**Effort:** Low-Medium  
**Risk:** Very Low  

**Verdict:** **Positive ROI.** This is a relatively easy feature with good community value.

---

## Recommended Timeline

### Week 1: Setup & Development
- **Day 1 (Morning):** Discord app registration, asset upload
- **Day 1 (Afternoon):** SDK integration, basic presence test
- **Day 2 (Morning):** Activity tracking system implementation
- **Day 2 (Afternoon):** Integration with game state systems
- **Day 3 (Morning):** User settings, polish, edge cases
- **Day 3 (Afternoon):** Testing (Windows primary)

### Week 2: Polish & Release
- **Day 4:** Mac/Linux testing (if platforms available)
- **Day 5:** Bug fixes, final polish
- **Release:** Include in next PC build

**Total Time:** 3-5 days

---

## Discord Overlay - Full Assessment

### Why Not Recommended

1. **Deprecated Official SDK**
   - Discord Game SDK deprecated in 2022
   - No official path for custom overlay integration

2. **Auto-Detection Only**
   - Discord client auto-detects games
   - No control over overlay behavior
   - Can't programmatically enable/disable

3. **Limited Value for DLS**
   - Single-player game (no multiplayer coordination)
   - Puzzle/creative genre (less real-time communication)
   - Alt+Tab works fine for desktop

4. **High Development Cost**
   - 5-10 days of work (vs 1-3 for Rich Presence)
   - Complex native library integration
   - Platform-specific build issues
   - Limited documentation (deprecated)

5. **Maintenance Burden**
   - Deprecated SDK may break in future
   - No official support from Discord
   - Would need to maintain native bindings

### If You Still Want Overlay...

**The Reality:** Overlay will likely "just work" if Discord recognizes DLS.exe as a game.

**How to Enable (User-Side):**
1. User adds DLS to Discord manually
2. Discord Game Activity detection
3. Overlay auto-activates

**No developer integration needed** for basic overlay support!

**What you CAN'T control:**
- Programmatically show/hide overlay
- Customize overlay appearance
- Detect if overlay is active

**Recommendation:** Let Discord auto-detect the game. Users who want overlay will enable it themselves. Focus development time on Rich Presence instead.

---

## Proof of Concept - Next Steps

If you want to proceed with a quick proof of concept:

### PoC Scope (4-8 hours)
1. Register Discord application
2. Download discord-rpc-csharp
3. Create minimal `DiscordRichPresenceManager`
4. Hard-code a test presence (e.g., "Testing Discord Integration")
5. Verify it shows in Discord

### PoC Success Criteria
- [ ] Discord app created
- [ ] Test presence visible in Discord client
- [ ] No errors/crashes
- [ ] Works on Windows

**If PoC successful:** Proceed with full implementation.  
**If PoC has issues:** Re-evaluate or consider paid Asset Store plugin.

---

## Community Engagement

### Original Request
> @Imred_Gemu asked: "Can we see Discord overlay in DLS and show DLS activity in Discord status?"

### Response to Community

**✅ Discord Activity Status:** Yes! This is very doable and we can implement it.  
**⚠️ Discord Overlay:** Technically limited (Discord deprecated custom integration), but Discord's auto-overlay may work.

### What to Tell Users

> "We're implementing Discord Rich Presence! Soon, your friends will be able to see what level you're playing or what circuit you're building right in Discord. Discord overlay may work automatically when you play DLS, but custom overlay integration is not possible due to Discord API changes. Rich Presence will be in the next PC update!"

---

## Final Recommendations

### ✅ DO THIS:
1. **Implement Discord Rich Presence** (1-3 days)
2. Use **discord-rpc-csharp** library (easiest path)
3. Show level name / chip name / game state
4. Add user setting to enable/disable
5. PC-only feature (Windows/Mac/Linux)
6. Event-driven OR polling approach (polling is simpler)

### ⚠️ CONSIDER:
1. Unity Asset Store plugin if you want pre-packaged solution
2. Event-driven updates for more responsive presence
3. Additional icons for different states (polish)

### ❌ DON'T DO:
1. Custom Discord Overlay integration (deprecated, high effort, low value)
2. Mobile Discord support (not possible)
3. Over-engineer the solution (keep it simple)

---

## Questions for Product Owner

Before implementation, clarify:

1. **Priority Level:** How important is this feature? (Affects timeline)
2. **Target Release:** Which build should include this? (PC only)
3. **User Setting:** Should Discord integration be opt-in or opt-out? (Recommend opt-out: enabled by default, can disable)
4. **Privacy:** Any concerns about showing activity? (Generally accepted in gaming community)
5. **Assets:** Who will create the Discord Rich Presence icons? (Designer needed)
6. **Budget:** Is 1-3 days of dev time approved for this feature?

---

## Conclusion

**Discord Rich Presence for Digital Logic Sim is a FEASIBLE and VALUABLE feature.**

- ✅ Technically straightforward
- ✅ Well-documented implementation path
- ✅ Minimal development time (1-3 days)
- ✅ Addresses community request
- ✅ Low maintenance burden
- ✅ Zero performance impact
- ✅ Good ROI

**Discord Overlay is NOT RECOMMENDED** due to deprecated SDK and limited value for single-player game.

**Recommendation:** **PROCEED with Discord Rich Presence implementation.** Assign 3-5 days for development + testing. Target next PC build for release.

---

## Appendix: Useful Links

### Official Discord Resources
- Discord Developer Portal: https://discord.com/developers
- Rich Presence Docs: https://discord.com/developers/docs/rich-presence/how-to
- Discord Game SDK (deprecated): https://discord.com/developers/docs/game-sdk/sdk-starter-guide

### Community Libraries
- discord-rpc-csharp: https://github.com/Lachee/discord-rpc-csharp
- Original discord-rpc: https://github.com/discord/discord-rpc

### Unity Asset Store
- Search "Discord Rich Presence" for pre-packaged plugins

### Examples & Tutorials
- Unity Discord Integration Tutorial: (Search YouTube/Google)
- Other Unity games using Rich Presence: Many examples on GitHub

---

**Report Compiled By:** AI Assistant  
**Date:** October 17, 2025  
**Status:** Ready for Review & Decision  
**Recommended Action:** Approve for Implementation

