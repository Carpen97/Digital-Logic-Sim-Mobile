# Ticket 044: Implementation Steps - Quick Reference

## 🔴 CRITICAL: Unity Security Patch CVE-2025-59489

---

## Quick Summary

**Current Version**: Unity 6000.0.46f1  
**Target Version**: Unity 6000.2.6f2  
**Platforms to Rebuild**: Android (primary), iOS, Windows, Linux  
**Timeline**: 5-7 days  

---

## Step-by-Step Implementation

### Step 1: Download Unity 6000.2.6f2

#### Option A: Unity Hub (Recommended)
```
1. Open Unity Hub
2. Go to "Installs" tab
3. Click "Install Editor"
4. Select "Unity 6000.2.6f2" from list
5. Select modules:
   - Android Build Support
   - iOS Build Support
   - Windows Build Support (IL2CPP)
   - Linux Build Support (IL2CPP)
6. Click "Install"
7. Wait for download and installation
```

#### Option B: Unity Download Archive
```
1. Visit: https://unity.com/releases/editor/archive
2. Find Unity 6000.2.6f2
3. Download installer for your OS
4. Run installer
5. Select required modules during installation
```

---

### Step 2: Backup Current Project

```powershell
# Create backup folder
mkdir I:\Programmering\UNITY_PROJECTS\DigitalLogicSimMobile\DLS_Backup_PreSecurity

# Copy project (excluding Library, Temp, Logs)
# Manual copy via File Explorer recommended for Windows
```

**What to backup**:
- Assets/
- ProjectSettings/
- Packages/
- Current build artifacts (AAB, APK files)

---

### Step 3: Open Project in Unity 6000.2.6f2

```
1. Open Unity Hub
2. Locate "Digital-Logic-Sim" project
3. Click dropdown next to "Open"
4. Select "Unity 6000.2.6f2"
5. Unity will open and may upgrade project files
6. Wait for project to fully load
7. Check Console for errors
```

**Expected behavior**:
- Project loads successfully
- Console may show warnings (review them)
- Scripts should compile without errors

---

### Step 4: Verify Compatibility

#### In Unity Editor:
```
1. Window → Console
   - Check for red errors (fix if any)
   - Review yellow warnings (note for testing)

2. File → Build Settings
   - Verify Android is available
   - Verify iOS is available
   - Verify Standalone (Windows/Linux) available

3. Press Play in Editor
   - Test basic functionality
   - Load a level
   - Test wire placement
   - Test component creation
   - Exit play mode
```

#### Check Firebase:
```
1. In Unity Editor Console, look for:
   - Firebase initialization messages
   - No Firebase errors
   
2. Test in Play mode if possible
   - Check if Firebase connects
```

---

### Step 5: Update Version Numbers

#### Open: ProjectSettings/ProjectSettings.asset

**Current versions**:
- bundleVersion: 2.1.6.9
- AndroidBundleVersionCode: 18
- iPhone buildNumber: 1

**New versions for security patch**:
- bundleVersion: 2.1.6.10
- AndroidBundleVersionCode: 19
- iPhone buildNumber: 2

#### Update via Unity Editor:
```
1. Edit → Project Settings → Player
2. Android tab:
   - Bundle Version Code: 18 → 19
3. iOS tab:
   - Build: 1 → 2
4. Other Settings (shared):
   - Version: 2.1.6.9 → 2.1.6.10
5. File → Save Project
```

---

### Step 6: Build Android (Priority 1)

#### Build APK for Testing:
```
1. File → Build Settings
2. Select "Android" platform
3. Click "Switch Platform" (if not already)
4. Uncheck "Build App Bundle (Google Play)"
5. Click "Build"
6. Save as: Digital-Logic-Sim-Mobile-TEST.apk
7. Wait for build (may take 10-30 minutes)
```

#### Test APK:
```
1. Transfer APK to Android device
2. Install and launch
3. Run through testing checklist:
   - App launches ✓
   - Firebase connects ✓
   - Leaderboard loads ✓
   - Can create and save levels ✓
   - Can load existing levels ✓
   - No crashes ✓
```

#### Build AAB for Google Play:
```
1. File → Build Settings
2. Select "Android" platform
3. Check "Build App Bundle (Google Play)"
4. Click "Build"
5. Save as: Digital-Logic-Sim-Mobile-2.1.6.10.aab
6. Wait for build
```

---

### Step 7: Build iOS (Priority 2)

```
1. File → Build Settings
2. Select "iOS" platform
3. Click "Switch Platform"
4. Click "Build"
5. Select output folder (e.g., DLS_IOS_Patched)
6. Wait for Unity to generate Xcode project
7. Open generated Xcode project
8. In Xcode:
   - Select target device/simulator
   - Product → Archive
   - Follow App Store upload process
```

**Note**: Requires macOS with Xcode installed

---

### Step 8: Build Windows (Priority 3)

```
1. File → Build Settings
2. Select "Windows Standalone" platform
3. Target Platform: Windows
4. Architecture: x86_64
5. Click "Build"
6. Save as: DLS_PC_Patched/
7. Wait for build (may take 10-20 minutes)
```

**Test Windows build**:
```
1. Navigate to output folder
2. Run Digital-Logic-Sim-Mobile.exe
3. Test basic functionality
4. Verify no crashes
```

---

### Step 9: Build Linux (Priority 4)

```
1. File → Build Settings
2. Select "Linux Standalone" platform
3. Target Platform: Linux
4. Architecture: x86_64
5. Click "Build"
6. Save as: DLS_Linux_Patched/
7. Wait for build
```

**Test Linux build** (if Linux system available):
```
1. Copy build to Linux machine
2. chmod +x DLS_Linux.x86_64
3. ./DLS_Linux.x86_64
4. Test basic functionality
```

---

### Step 10: Upload to Google Play

#### Prepare:
```
1. Have AAB file ready: Digital-Logic-Sim-Mobile-2.1.6.10.aab
2. Log into Google Play Console
3. Navigate to your app
```

#### Upload:
```
1. Production → Create new release
2. Upload AAB file
3. Release name: "2.1.6.10 - Security Update"
4. Release notes:
   "Version 2.1.6.10 - Critical Security Update
   • Security patch for CVE-2025-59489
   • Updated Unity engine to latest secure version
   • No functional changes or new features
   • Recommended update for all users"
5. Save
6. Review and Rollout
```

**Timeline**: Google Play review typically takes 1-3 days

---

### Step 11: Update Other Platforms

#### iOS (if distributed via App Store):
```
1. Upload to App Store Connect via Xcode
2. Update version info
3. Submit for review
```

#### Windows/Linux (if distributed elsewhere):
```
1. Upload to itch.io (if applicable)
2. Update GitHub releases (if applicable)
3. Update any download links
```

---

## Testing Checklist (Android - Primary)

Before uploading to Google Play, verify:

### Basic Functionality
- [ ] App launches without crash
- [ ] Main menu displays correctly
- [ ] UI elements are responsive
- [ ] No visual glitches

### Firebase Integration
- [ ] Firebase initializes (check logs)
- [ ] Authentication works
- [ ] Firestore reads/writes work
- [ ] Leaderboard loads
- [ ] Scores can be submitted

### Level System
- [ ] Can create new level
- [ ] Can save level
- [ ] Can load existing levels
- [ ] Level selection menu works
- [ ] Tutorial levels load

### Core Gameplay
- [ ] Can place components
- [ ] Can create wires
- [ ] Can delete components/wires
- [ ] Logic simulation works
- [ ] Can test circuits
- [ ] Performance is acceptable

### Settings & Persistence
- [ ] Settings save correctly
- [ ] Progress persists across sessions
- [ ] No data loss

---

## Troubleshooting

### Build Errors

**"Unable to find Android SDK"**
```
Fix: File → Preferences → External Tools → Android SDK Tools
Set correct path to Android SDK
```

**"IL2CPP build failed"**
```
Fix: Check console for specific error
Common causes:
- Missing Android NDK
- Insufficient disk space
- Corrupted cache (delete Library/Il2cppBuildCache)
```

**"Keystore not found"**
```
Fix: Publishing Settings → Keystore
Set path to: Desktop/DLS.keystore
Enter keystore password
```

### Firebase Errors

**"Firebase not initializing"**
```
Check:
1. google-services.json is in Assets/
2. Firebase SDK is properly imported
3. Internet connection available
4. Firebase Console shows app is active
```

### Performance Issues

**"Build is slower than before"**
```
- Expected after Unity upgrade
- Run on actual device, not simulator
- Check IL2CPP optimization settings
- Compare build size to previous version
```

---

## Rollback Plan (If Needed)

If critical issues arise:

```
1. Close Unity
2. Open Unity Hub
3. Open project with Unity 6000.0.46f1 (original version)
4. Rebuild with original version
5. Keep original builds available for emergency rollback
```

**Note**: Keep old builds available until new builds are verified in production

---

## Success Verification

### You've succeeded when:
- ✅ Unity upgraded to 6000.2.6f2
- ✅ All platforms build without errors
- ✅ Android APK tested on physical device
- ✅ AAB uploaded to Google Play
- ✅ iOS build submitted (if applicable)
- ✅ PC/Linux builds updated and tested
- ✅ No regressions in functionality
- ✅ No new crashes or bugs
- ✅ ProjectVersion.txt shows 6000.2.6f2

---

## Time Estimates

| Task | Estimated Time |
|------|----------------|
| Download Unity | 30-60 min |
| Project backup | 15-30 min |
| Open in new Unity | 10-20 min |
| Verify compatibility | 30-60 min |
| Build Android APK | 15-30 min |
| Test Android APK | 30-60 min |
| Build Android AAB | 15-30 min |
| Build iOS | 20-40 min |
| Build Windows | 15-30 min |
| Build Linux | 15-30 min |
| Upload to Google Play | 15-30 min |
| **Total** | **~4-7 hours** |

*Times are estimates; actual time may vary based on hardware and network speed*

---

## Commands Reference

### Git Commands (For After Completion)
```powershell
# Check status
git status

# Stage changes
git add ProjectSettings/ProjectVersion.txt
git add ProjectInstructions/Tickets/Ticket_044*.md

# Commit
git commit -m "Ticket 044: Unity Security Patch CVE-2025-59489 - Upgraded to Unity 6000.2.6f2"

# Push
git push origin main
```

### PowerShell Commands (Windows)
```powershell
# Check Unity Hub installations
Get-ChildItem "C:\Program Files\Unity\Hub\Editor"

# Check file size
Get-ChildItem -Path "*.aab" | Select-Object Name, Length

# Calculate hash (for verification)
Get-FileHash .\Digital-Logic-Sim-Mobile-2.1.6.10.aab -Algorithm SHA256
```

---

## Contact & Resources

### Unity Resources
- Unity Hub: [Download](https://unity.com/download)
- Unity Archive: [unity.com/releases/editor/archive](https://unity.com/releases/editor/archive)
- Security Advisory: [activation.unity3d.com/security/sept-2025-01](https://activation.unity3d.com/security/sept-2025-01)

### Distribution Platforms
- Google Play Console: [play.google.com/console](https://play.google.com/console)
- App Store Connect: [appstoreconnect.apple.com](https://appstoreconnect.apple.com)

### CVE Information
- NVD: [nvd.nist.gov/vuln/detail/CVE-2025-59489](https://nvd.nist.gov/vuln/detail/CVE-2025-59489)

---

**Document Version**: 1.0  
**Created**: 2025-10-12  
**For Ticket**: 044 - Unity Security Vulnerability Patch  
**Priority**: CRITICAL - TIME SENSITIVE

