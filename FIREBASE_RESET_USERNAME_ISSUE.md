# Username Update Error After Firebase Reset

## Problem
After deleting all Firebase collections, users get the error **"Failed to update username. Please try again"** when trying to set/update their username.

## Root Cause
When you deleted all Firebase collections, you deleted:
- The `users` collection (user profiles)
- The `usernames` collection (username reservations)

However, your phone still has **cached data** locally (in PlayerPrefs/local storage) that remembers your old username. This creates a conflict:

1. App loads your old username from local cache → Sets `_originalUserName` to your old username
2. App tries to load your profile from Firebase → Returns `null` (because you deleted it)
3. App sees you had a username locally but no profile in Firebase → Confused state
4. When you try to set a username, the app thinks you're **changing** your username (not claiming it for the first time)
5. `ChangeUsernameAsync` requires an existing profile → Fails with "No profile found"

## Solution: Clear App Data on Your Phone

You need to clear the app's local cache to match the Firebase reset:

### For Android:
1. **Settings** → **Apps** → **Digital Logic Sim**
2. Tap **Storage**
3. Tap **Clear Data** or **Clear Storage**
4. Confirm

### For iOS:
1. **Settings** → **General** → **iPhone Storage**
2. Find **Digital Logic Sim**
3. Tap **Delete App**
4. Reinstall from TestFlight/App Store

### Alternative: In-App Cache Clear
If the app has a "Clear Cache" or "Reset" option in settings, use that instead.

## After Clearing:
1. Open the app
2. Go to upload a score
3. Enter your username (any username, including your old one - it's available again!)
4. The app will **claim** the username (not change it)
5. ✅ It will work!

## Technical Details

The app has two different username flows:

### First-Time Username Claim (`ClaimUsernameAsync`):
- Creates a new user profile
- Reserves the username
- Works when you have no profile

### Username Change (`ChangeUsernameAsync`):
- Updates existing profile
- Releases old username
- Reserves new username
- Updates all leaderboard entries
- **Requires existing profile** ← This is why it fails

After Firebase reset, you need to use the **claim flow**, but the app tries to use the **change flow** because of cached data.

## Prevention for Future Resets

If you reset Firebase in the future and want to avoid this issue:

### Option 1: Add a Cache Clear Button in App
Add a "Clear Local Data" button that calls:
```csharp
// Clear username cache
PlayerPrefs.DeleteKey("UserName");
PlayerPrefs.DeleteKey("RememberName");
PlayerPrefs.Save();

// Clear user auth cache
UserAuthService.ClearCache();
```

### Option 2: Make Change Flow More Robust
Update `ChangeUsernameAsync` to fall back to `ClaimUsernameAsync` if no profile exists:

```csharp
// Get current profile
var currentProfile = await GetCurrentUserProfileAsync();
if (currentProfile == null)
{
    // No profile exists - use claim instead of change
    return await ClaimUsernameAsync(newUsername);
}
```

### Option 3: Reset Instructions for Users
When resetting Firebase, also provide instructions to users to clear app data.

## Quick Fix Summary

**What you need to do right now:**
1. Clear app data on your phone (Settings → Apps → Digital Logic Sim → Storage → Clear Data)
2. Reopen the app
3. Try setting your username again
4. ✅ It should work!

The error message "Failed to update username" is misleading - it should say "Failed to update username. Please clear app data or try a different username."

