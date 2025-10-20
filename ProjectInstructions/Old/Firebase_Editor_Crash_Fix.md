# Firebase Editor Crash Fix - Android Build Target

## Problem Summary

**Date**: October 11, 2025  
**Issue**: Unity Editor crashes when opening LeaderboardPopup with Android as the build platform

## Root Cause

The crash occurs due to a **known issue** with Firebase C++ SDK (`FirebaseCppApp-13_2_0.dll`) when running in Unity Editor on Windows with Android as the build target. The crash happens specifically in the WebSocket upgrade code when Firebase tries to establish a Firestore connection.

### Crash Location
```
Stack trace shows crash in:
0x00007FFB0DBDE303 (FirebaseCppApp-13_2_0) uWS::HttpSocket<0>::upgrade
```

### When Does It Happen?
1. Unity Editor is open
2. Build platform is set to **Android** (or iOS)
3. User opens LeaderboardPopup
4. Firebase initializes successfully
5. Firestore tries to connect → **CRASH**

## The Solution

Modified `LeaderboardService.cs` to automatically detect Editor + Mobile platform combination and use local storage instead of Firebase:

### Changes Made

**File**: `Assets/Scripts/Online/LeaderboardService.cs`

Changed `UseLocalStorageInEditor` from a simple field to a smart property that automatically returns `true` when:
- Running in Unity Editor AND
- Build target is Android or iOS

```csharp
public static bool UseLocalStorageInEditor
{
    get
    {
#if UNITY_EDITOR
        // CRITICAL: Firebase C++ SDK crashes on Windows Editor when build target is Android
        // Automatically use local storage in Editor for mobile platforms to prevent crashes
        var buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
        return buildTarget == UnityEditor.BuildTarget.Android || 
               buildTarget == UnityEditor.BuildTarget.iOS;
#else
        return false;
#endif
    }
}
```

## How It Works Now

### In Editor with Android/iOS Build Target:
- ✅ LeaderboardPopup opens without crash
- ✅ Uses `EditorLocalStorage` for mock leaderboard data
- ✅ No Firebase initialization attempted
- ✅ Full UI testing possible

### In Editor with PC/Standalone Build Target:
- ✅ LeaderboardPopup connects to real Firebase
- ✅ Can test actual Firebase integration
- ✅ No crashes (Firebase C++ SDK works on Windows standalone target)

### In Actual Builds (Android/iOS/PC):
- ✅ Always uses real Firebase
- ✅ No local storage fallback
- ✅ Full leaderboard functionality

## Testing the Fix

### To Test Leaderboard UI in Editor with Android:
1. Switch build platform to Android
2. Enter Play mode
3. Open any level
4. Click "Leaderboard" button
5. ✅ **No crash** - see mock leaderboard data from `EditorLocalStorage`

### To Test Real Firebase in Editor:
1. Switch build platform to **PC, Mac & Linux Standalone**
2. Enter Play mode
3. Open any level
4. Click "Leaderboard" button
5. ✅ Connects to real Firebase (requires internet)

## Related Code

- **Firebase Bootstrap**: `Assets/Scripts/Online/FirebaseBootstrap.cs`
- **Leaderboard Service**: `Assets/Scripts/Online/LeaderboardService.cs`
- **Editor Mock Storage**: `Assets/Scripts/Online/EditorLocalStorage.cs`
- **Leaderboard UI**: `Assets/Scripts/Graphics/UI/Menus/LeaderboardPopup.cs`

## Known Limitations

- Cannot test real Firebase integration in Editor when Android is the build target
- Must switch to Standalone to test Firebase connectivity
- EditorLocalStorage provides mock data only (does not persist between Editor sessions)

## Firebase SDK Issue References

This is a known Firebase issue:
- Firebase C++ SDK has compatibility issues with Unity Editor on Windows when mobile platforms are active
- The SDK expects native Android/iOS environment, not Windows with Android emulation
- Workaround is to use mock data in Editor for mobile platforms

## Future Improvements

If Firebase releases an updated SDK that fixes Windows Editor + Android platform compatibility, this workaround can be removed by simply making `UseLocalStorageInEditor` return `false` by default again.

