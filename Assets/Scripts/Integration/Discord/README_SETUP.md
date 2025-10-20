# Discord Rich Presence Setup Instructions

## ⚠️ IMPORTANT: DLL Missing!

The `DiscordRPC.dll` file is required but currently missing. Follow these steps:

### Step 1: Download DiscordRPC.dll

**Option A: Direct Download (Easiest)**
1. Go to: https://github.com/Lachee/discord-rpc-csharp/releases
2. Download the latest release (e.g., `DiscordRPC.1.2.1.nupkg`)
3. Rename `.nupkg` to `.zip` and extract it
4. Navigate to `lib/netstandard2.0/` folder
5. Copy `DiscordRPC.dll` to: `Assets/Plugins/DiscordRPC/DiscordRPC.dll`

**Option B: NuGet Package Manager**
```bash
Install-Package DiscordRichPresence
```
Then extract DLL from packages folder.

### Step 2: Configure DLL in Unity

1. In Unity, select `Assets/Plugins/DiscordRPC/DiscordRPC.dll`
2. In the Inspector, set platform compatibility:
   - ✅ **Standalone** (Windows/Mac/Linux)
   - ❌ Android
   - ❌ iOS
   - ❌ WebGL
3. Click "Apply"

### Step 3: Discord Application Setup

1. Go to https://discord.com/developers/applications
2. Click "New Application"
3. Name: **"Digital Logic Sim"**
4. **Copy the Application ID** (looks like: `1300166829308756030`)
5. Open `DiscordRichPresenceManager.cs`
6. Replace `APPLICATION_ID` constant with your actual ID

### Step 4: Upload Discord Assets

Upload these icons to Discord Developer Portal → Rich Presence → Art Assets:

| Asset Key Name | Description | Size |
|----------------|-------------|------|
| `dls_logo` | Main game logo | 512x512 |
| `icon_sandbox` | Sandbox mode icon | 256x256 |
| `icon_level` | Level mode icon | 256x256 |
| `icon_menu` | Menu icon | 256x256 |
| `icon_leaderboard` | Leaderboard icon | 256x256 |

**Asset names must match exactly!**

### Step 5: Test

1. Make sure Discord is running on your PC
2. Start the game
3. Check Discord - you should see "Playing Digital Logic Sim"

---

## Files in this Integration

- **DiscordRichPresenceManager.cs** - Core Discord connection manager
- **DiscordActivityTracker.cs** - Automatic game state tracking
- This file: Setup instructions

## Troubleshooting

**"Discord not showing"**
- Verify Discord is running
- Check Application ID is correct
- Enable "Display current activity" in Discord Settings → Activity Privacy

**"Compilation errors"**
- Make sure DLL is in correct location
- Check platform settings on DLL
- Verify `#if DISCORD_SUPPORTED` conditionals

**"Assets not showing"**
- Asset keys are case-sensitive
- Wait 5-10 minutes for Discord to propagate assets
- Clear Discord cache if needed

---

## Current Status

✅ C# integration scripts created  
❌ DiscordRPC.dll needs to be downloaded  
❌ Discord Application ID needs to be configured  
❌ Discord assets need to be uploaded

Once these are complete, Discord Rich Presence will work!

