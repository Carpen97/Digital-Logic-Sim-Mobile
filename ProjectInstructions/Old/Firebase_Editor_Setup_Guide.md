# Firebase in Unity Editor - Setup & Testing Guide

**Status:** ✅ IMPLEMENTED  
**Date:** October 11, 2025  
**Ticket:** 050

---

## Overview

Firebase (Authentication, Firestore, Storage) now works in Unity Editor on Windows! This dramatically speeds up development and testing.

### Before vs After
- **Before:** 5-10 minutes per test (full PC build required)
- **After:** 10-30 seconds (just press Play in Editor)
- **Speed Improvement:** 10-30x faster! 🚀

---

## What Was Fixed

### 1. Code Changes
✅ **FirebaseBootstrap.cs** - Removed `#if UNITY_EDITOR` block that prevented initialization  
✅ **FirebaseProbe.cs** - Updated to use FirebaseBootstrap for all platforms  
✅ **LeaderboardService.cs** - Added `UseLocalStorageInEditor` flag (default: false = use real Firebase)

### 2. Plugin Configuration (THE KEY FIX!)
✅ **Enabled Firebase DLLs in Editor** - Updated `.meta` files for:
- `FirebaseCppApp-13_2_0.dll` 
- `FirebaseCppAuth.dll`
- `FirebaseCppFirestore.dll`
- `FirebaseCppStorage.dll`

Changed from:
```yaml
Editor:
  enabled: 0  # ❌ Was disabled - caused DllNotFoundException
```

To:
```yaml
Editor:
  enabled: 1  # ✅ Now enabled for Windows Editor
  CPU: x86_64
  OS: Windows
```

### 3. Configuration Files
✅ **google-services-desktop.json** - Already properly configured in `StreamingAssets/`

---

## How to Use

### Method 1: Real Firebase (Default)
Just press Play in Unity Editor! Firebase will connect automatically.

```csharp
// LeaderboardService.UseLocalStorageInEditor is false by default
// So you get real Firebase connectivity
```

### Method 2: Mock Local Storage (For Offline Development)
If you want to test WITHOUT internet connection:

```csharp
// At app startup (e.g., in Main.cs or similar)
DLS.Online.LeaderboardService.UseLocalStorageInEditor = true;
```

---

## Testing Firebase in Editor

### Quick Test 1: Check Initialization
1. Open Unity Editor
2. Press Play
3. Look for logs in Console:
   ```
   [Firebase] Starting Firebase initialization...
   [Firebase] Dependencies: Available
   [Firebase] Firebase app initialized successfully
   [Firebase] Anonymous authentication successful. UID: <your-uid>
   ```

### Quick Test 2: Test Leaderboard
1. Complete a level
2. Submit score
3. Check Console for:
   ```
   [Leaderboard] Editor mode with real Firebase enabled - connecting to Firebase
   [Leaderboard] Successfully saved score for level <level-id>
   ```
4. Verify in Firebase Console that the score appears in Firestore

### Quick Test 3: Test Authentication
1. Play in Editor
2. Sign in (anonymous auth happens automatically)
3. Check Firebase Console > Authentication tab
4. You should see a new anonymous user

---

## Troubleshooting

### Issue: "DllNotFoundException: FirebaseCppApp-13_2_0"
**Solution:** The Firebase DLL .meta files need to be re-imported by Unity.
1. Close Unity Editor
2. Delete `Library/` folder
3. Reopen Unity (will reimport all assets)
4. Try again

### Issue: "Firebase dependencies not available"
**Possible causes:**
1. Internet connection required for first initialization
2. Firewall blocking Firebase SDK
3. Antivirus blocking DLL loading

**Solution:**
1. Check internet connection
2. Temporarily disable antivirus
3. Check firewall rules for Unity Editor

### Issue: Firebase still uses mock storage in Editor
**Solution:** Check that `LeaderboardService.UseLocalStorageInEditor` is `false`:
```csharp
Debug.Log($"Using local storage: {LeaderboardService.UseLocalStorageInEditor}");
```

### Issue: "Firestore write operation timed out"
**Known Issue:** Firebase C++ SDK on Windows can occasionally crash during Firestore writes.

**Workaround implemented:**
- Firestore persistence is disabled on Windows (`db.Settings.PersistenceEnabled = false`)
- Timeout protection (30 seconds)
- Graceful error handling

If you still see crashes, use `UseLocalStorageInEditor = true` for that session.

---

## Platform-Specific Notes

### Windows (Primary Development Platform)
✅ **Fully Supported**
- All Firebase features work
- x86_64 DLLs used
- Firestore persistence disabled to prevent crashes

### Mac Editor
⚠️ **Should work but not tested**
- Would need .bundle files enabled in Editor
- May need separate configuration

### Linux Editor  
⚠️ **Should work but not tested**
- Would need .so files enabled in Editor
- May need separate configuration

---

## Performance Tips

### Faster Iteration
1. Keep Editor window open during development
2. No need to build to PC anymore for Firebase testing
3. Use domain reload disabled for even faster play mode entry

### Debugging Firebase Issues
Enable detailed logging:
```csharp
FirebaseApp.LogLevel = LogLevel.Debug; // Already set in FirebaseBootstrap
```

### Network Request Inspection
Use Fiddler or Wireshark to see Firebase API calls if needed.

---

## Security Considerations

### Development Firebase Project
Consider using a **separate Firebase project for development**:
- Prevents polluting production data
- Allows testing destructive operations
- Can have relaxed security rules

### Firebase Security Rules
Your Firebase Console security rules apply to Editor mode too!
- Use Authentication UID-based rules
- Test with production-like rules when possible

---

## Known Limitations

### 1. Firestore Persistence
- Disabled on Windows Desktop/Editor to prevent crashes
- This is a known Firebase SDK limitation
- Data is still cached in memory during the session

### 2. Firebase Storage
- Not extensively tested in Editor yet
- Should work based on SDK support
- Test before relying on it

### 3. Authentication Methods
- **Anonymous Auth:** ✅ Works perfectly
- **Email/Password:** ⚠️ Should work, not tested
- **Google Sign-In:** ❌ Likely won't work (no web browser integration in Editor)
- **Other providers:** ❌ Likely won't work in Editor

### 4. Platform Detection
Firebase sees Editor as Windows Standalone platform.

---

## Rollback Instructions

If Firebase in Editor causes issues, you can quickly rollback:

### Option 1: Use Local Storage Mode
```csharp
LeaderboardService.UseLocalStorageInEditor = true;
```

### Option 2: Revert Code Changes
```bash
git checkout origin/main -- Assets/Scripts/Online/FirebaseBootstrap.cs
git checkout origin/main -- Assets/FirebaseProbe.cs
git checkout origin/main -- Assets/Scripts/Online/LeaderboardService.cs
```

### Option 3: Disable Firebase DLLs in Editor
Revert the `.meta` files:
```bash
git checkout origin/main -- Assets/Firebase/Plugins/x86_64/*.meta
```

Then restart Unity Editor.

---

## Future Improvements

### Potential Enhancements
1. ☐ Add Editor window for Firebase connection status
2. ☐ Add button to toggle between real Firebase and mock storage
3. ☐ Add Firebase emulator support for true offline development
4. ☐ Test and document Mac/Linux Editor support
5. ☐ Add authentication provider testing in Editor
6. ☐ Create automated tests for Firebase functionality

### Firebase Emulator Suite (Advanced)
For completely offline development:
1. Install Firebase CLI
2. Run `firebase emulators:start`
3. Configure Unity to use localhost endpoints
4. Get 100% offline development capability

---

## Success Metrics

### Development Speed
- ✅ Test iterations: 10-30 seconds (was 5-10 minutes)
- ✅ No build step required for Firebase testing
- ✅ Immediate feedback on Firebase operations

### Reliability
- ✅ No regressions in PC builds
- ✅ No regressions in mobile builds
- ✅ Graceful fallback to mock storage if needed

---

## References

- **Firebase Unity SDK Docs:** https://firebase.google.com/docs/unity/setup
- **Known Windows Issues:** https://github.com/firebase/quickstart-unity/issues/1284
- **Desktop Configuration:** https://firebase.google.com/docs/unity/setup#desktop-workflow

---

## Support

If you encounter issues:
1. Check this guide's Troubleshooting section
2. Check Unity Console for detailed error messages
3. Check Firebase Console for connectivity
4. Use `LeaderboardService.UseLocalStorageInEditor = true` as a temporary workaround
5. Document the issue for future reference

---

**Last Updated:** October 11, 2025  
**Tested On:** Unity 2022.3.x, Windows 10/11, Firebase Unity SDK 13.2.0

