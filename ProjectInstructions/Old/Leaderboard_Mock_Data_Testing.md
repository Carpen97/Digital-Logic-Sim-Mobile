# Leaderboard Mock Data for Graphics Testing

## Overview

When testing the leaderboard UI on Android in the Unity Editor, mock data is automatically generated to help you visualize how the leaderboard will look with real data.

## Automatic Mock Data Generation

The system automatically creates mock leaderboard data when:
- You're in **Unity Editor**
- Build platform is **Android or iOS**
- You open a leaderboard for the first time

### What Mock Data Is Included

**Levels**: Mock scores are generated for these levels:
- NOT Gate
- AND Gate
- OR Gate
- XOR Gate
- NAND Gate
- NOR Gate

**Usernames**: A variety of usernames to test different UI scenarios:
- Short names: `Alice`, `Bob`, `Charlie`, `Diana`, `Eve`, etc.
- Long names: `Katherine_with_a_really_long_name_to_test_truncation`
- Very long names: `VeryLongUserNameThatShouldBeTruncatedInTheUI`
- Special characters: `短名` (Chinese), `🎮Gamer42` (emoji)
- Generic names: `Player_12345`, `Anonymous`

**Scores**: 
- Each level has 10-15 mock entries
- Scores range from 5 to ~35 (lower is better)
- Realistic distribution with slight randomization

**Timestamps**:
- Distributed over the past 7 days
- Proper UTC formatting for date display testing

**Complete Solutions**:
- Every 3rd entry has a mock "completeSolutionId"
- Tests the "VIEW" button enable/disable logic

## How to Test Leaderboard Graphics on Android

### Step 1: Set Up the Environment
1. Open Unity Editor
2. Switch build platform to **Android** (File → Build Settings → Android → Switch Platform)
3. Wait for Unity to finish re-importing assets

### Step 2: Enter Play Mode
1. Click the Play button in Unity Editor
2. Wait for the game to load
3. The mock data system will initialize automatically

### Step 3: Open Leaderboard
1. Navigate to any level (e.g., "NOT Gate")
2. Click the **"Leaderboard"** button
3. ✅ **The leaderboard should open WITHOUT crashing**

### Step 4: Test Different Scenarios

**Test the top 10 display:**
- Verify all 10 entries are visible
- Check rank numbers (#1, #2, #3, etc.)
- Verify score values are displayed correctly
- Check username alignment and truncation

**Test long usernames:**
- Look for entries with very long usernames
- Verify they're truncated properly with "..."
- Check they don't overflow the column

**Test special characters:**
- Find entries with emojis or Unicode characters
- Verify they display correctly
- Check alignment isn't broken

**Test date formatting:**
- Verify dates show as "MM/dd HH:mm"
- Check recent vs older timestamps
- Ensure dates are readable

**Test selection:**
- Click on different leaderboard entries
- Verify selection highlight works
- Check the "VIEW" button enables for entries with solutions (every 3rd entry)

**Test scrolling** (if you have >10 entries visible):
- Try scrolling the leaderboard
- Verify smooth scrolling behavior
- Check no visual glitches

## Unity Editor Menu Commands

Access these from the Unity menu bar: **DLS → Mock Data**

### Regenerate Leaderboard Data
```
DLS → Mock Data → Regenerate Leaderboard Data
```
- Clears existing mock data
- Generates fresh mock data with randomized usernames and scores
- Useful for testing different visual layouts

### Clear All Mock Data
```
DLS → Mock Data → Clear All Mock Data
```
- Deletes all mock leaderboard data
- Next time you open leaderboard in Play mode, it will regenerate automatically

### Show Mock Data Stats
```
DLS → Mock Data → Show Mock Data Stats
```
- Shows how many mock scores are currently stored
- Displays the storage location on disk

## Storage Location

Mock data is stored at:
```
{Application.persistentDataPath}/EditorLocalStorage/
  ├── scores.json
  └── solutions.json
```

On Windows, this is typically:
```
C:\Users\{YourName}\AppData\LocalLow\{CompanyName}\{ProjectName}\EditorLocalStorage\
```

## Testing Checklist

Use this checklist when reviewing leaderboard graphics:

- [ ] Leaderboard opens without crashing
- [ ] All 10 entries are visible
- [ ] Rank column shows #1, #2, #3, etc.
- [ ] Score column shows numeric values
- [ ] Username column displays names correctly
- [ ] Long usernames are truncated properly
- [ ] Date column shows "MM/dd HH:mm" format
- [ ] Emojis and special characters display correctly
- [ ] Selection highlighting works
- [ ] "VIEW" button enables for entries with solutions
- [ ] "CANCEL" button always enabled
- [ ] Scrolling works smoothly (if applicable)
- [ ] No text overflow or alignment issues
- [ ] Colors are readable and aesthetically pleasing
- [ ] Mobile touch targets are appropriately sized

## Switching to Real Firebase

If you want to test with **real Firebase data** in Editor:

1. Switch build platform to **"PC, Mac & Linux Standalone"**
2. Enter Play mode
3. Open leaderboard
4. ✅ Will connect to real Firebase (requires internet)

**Note**: Cannot test real Firebase when build platform is Android (would crash due to Firebase SDK issue).

## Mock Data Code Structure

The mock data system consists of:

1. **EditorLocalStorage.cs** - Manages mock data storage
   - `Initialize()` - Auto-creates mock data if empty
   - `InitializeMockData()` - Generates realistic test data
   - `GetTopScores()` - Returns mock scores for a level
   - `RegenerateMockData()` - Refresh mock data

2. **LeaderboardService.cs** - Smart Firebase/Mock switcher
   - `UseLocalStorageInEditor` property - Auto-detects platform
   - Returns `true` for Android/iOS in Editor
   - Automatically uses mock data when appropriate

3. **LeaderboardMockDataMenu.cs** - Unity Editor utilities
   - Menu commands for managing mock data
   - Convenient regeneration and clearing

## Troubleshooting

**Problem**: No mock data appears in leaderboard

**Solution**:
1. Check Unity Console for `[EditorLocalStorage]` logs
2. Verify build platform is Android or iOS
3. Try manually regenerating: `DLS → Mock Data → Regenerate Leaderboard Data`

---

**Problem**: Leaderboard crashes in Editor

**Solution**:
1. Ensure build platform is Android (not Standalone)
2. Check `LeaderboardService.UseLocalStorageInEditor` returns `true` in Editor
3. Verify `EditorLocalStorage.Initialize()` is being called

---

**Problem**: Want to test different usernames

**Solution**:
1. Use `DLS → Mock Data → Regenerate Leaderboard Data`
2. Or edit `EditorLocalStorage.cs` → `InitializeMockData()` → `userNames` array
3. Regenerate the data

## Future Enhancements

Potential improvements for mock data system:
- [ ] UI to customize mock data generation (e.g., score ranges, username styles)
- [ ] Import real Firebase data snapshot for offline testing
- [ ] Mock data presets (sparse data, dense data, edge cases)
- [ ] Screenshot capture for design review

