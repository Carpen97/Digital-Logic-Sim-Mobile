# Actual Fix: Firebase Windows Crash (uWS::HttpSocket::upgrade)

**Problem:** PC build crashes when opening Project Sharing due to `uWS::HttpSocket::upgrade` in Firebase native code (firebase-unity-sdk#1291).

**Root cause:** MSVC ABI incompatibility with older Visual C++ runtimes. Firebase was built with a mutex that crashes on systems using older VC++ runtimes.

**Official fix:** Merged in [firebase-cpp-sdk PR #1814](https://github.com/firebase/firebase-cpp-sdk/pull/1814) (Jan 14, 2026). Adds `_DISABLE_CONSTEXPR_MUTEX_CONSTRUCTOR` so Firebase works with older runtimes.

**Fix in this project:** Upgrade Firebase Unity SDK from **13.2.0** → **13.7.0** (or 13.8.0). Those versions bundle Firebase C++ SDK 13.4.0+, which includes the fix.

---

## Upgrade Steps

### Option A: Full Firebase Upgrade (recommended)

1. **Close Unity** (required).

2. **Download Firebase Unity SDK 13.7.0:**
   - https://dl.google.com/firebase/sdk/unity/firebase_unity_sdk_13.7.0.zip

3. **Backup and replace:**
   - Rename `Assets/Firebase` to `Assets/Firebase_backup_13.2`
   - Rename `Assets/ExternalDependencyManager` to `Assets/ExternalDependencyManager_backup`
   - Extract the zip and copy the `Firebase` and `ExternalDependencyManager` folders from the extracted `FirebaseUnitySdk` into `Assets/`
   - Copy `Editor Default Resources/Firebase` if it exists in the extracted SDK

4. **Re-open Unity.** Let it import and resolve dependencies.

5. **Re-run CopyGoogleServices** if your project uses it (Assets → Google → Firebase → Copy google-services.json).

6. **Build and test** the PC build.

### Option B: Package Manager (if project uses UPM for Firebase)

Add to `Packages/manifest.json`:
```json
"com.google.firebase.app": "https://dl.google.com/games/registry/unity/com.google.firebase.app/com.google.firebase.app-13.7.0.tgz",
"com.google.firebase.auth": "https://dl.google.com/games/registry/unity/com.google.firebase.auth/com.google.firebase.auth-13.7.0.tgz",
"com.google.firebase.firestore": "https://dl.google.com/games/registry/unity/com.google.firebase.firestore/com.google.firebase.firestore-13.7.0.tgz",
"com.google.firebase.storage": "https://dl.google.com/games/registry/unity/com.google.firebase.storage/com.google.firebase.storage-13.7.0.tgz"
```

Then remove the old Firebase from Assets and let Package Manager install.

### If it still crashes

- Update [Visual C++ 2015–2022 Redistributable (x64)](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170) on the target PC.
- Some older CPUs (e.g. Intel Celeron N4120) lack BMI2; a separate build without BMI2 may be needed.
