# Leaderboard Level Name Display Fix

## Problem
The leaderboard was displaying the level ID (e.g., "lvl.not.1") instead of the level name (e.g., "NOT Gate") in the header.

## Solution
Updated the leaderboard to accept the level name as a parameter instead of looking it up:

### Changes Made:

1. **Updated `LeaderboardPopup.Open()` signature**:
   - Changed from: `Open(string levelId)`
   - Changed to: `Open(string levelId, string levelName = null)`
   - Falls back to levelId if levelName is not provided

2. **Updated call sites** to pass both parameters:
   - **LevelValidationPopup.cs**: Gets level ID and name from `LevelManager.Current`
   - **LevelsMenu.cs**: Gets level ID and name from selected level definition

3. **Simplified header display**: Shows `_levelName` directly (no conditional logic needed)

## Why This Approach is Better
- ✅ **No file I/O**: Doesn't need to load/parse `levels.json` every time
- ✅ **More efficient**: Caller already has the level definition
- ✅ **Simpler code**: Less error handling, fewer dependencies
- ✅ **More flexible**: Works even if level isn't in levels.json
- ✅ **Cleaner architecture**: Single responsibility - caller provides data

## Example
**Before**: 
- Header showed: `LEADERBOARD - lvl.not.1`

**After**:
- Header shows: `LEADERBOARD - NOT Gate`

## Files Modified
- `Assets/Scripts/Graphics/UI/Menus/LeaderboardPopup.cs`
- `Assets/Scripts/Graphics/UI/Menus/LevelValidationPopup.cs`
- `Assets/Scripts/Graphics/UI/Menus/LevelsMenu.cs`

## How It Works
1. User clicks "Leaderboard" button
2. Caller gets `LevelDefinition` from `LevelManager.Current` or selected level
3. Extracts both `id` and `name` from the definition
4. Calls `LeaderboardPopup.Open(levelId, levelName)`
5. Leaderboard displays: "LEADERBOARD - NOT Gate"

## Fallback Behavior
- If `levelName` parameter is null, uses `levelId` as fallback
- If level name is empty in definition, caller uses ID as fallback
- Always displays something meaningful

